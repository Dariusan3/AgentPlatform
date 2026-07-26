-- Activare pgvector pentru RAG
CREATE EXTENSION IF NOT EXISTS vector;

-- Tabela principala agenti (tenants)
-- id === auth.users.id, deci auth.uid() este direct tenant_id-ul
CREATE TABLE agents (
    id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    email VARCHAR(255) UNIQUE NOT NULL,
    full_name VARCHAR(200),
    company_name VARCHAR(200),
    phone VARCHAR(20),
    plan VARCHAR(20) DEFAULT 'starter',
    stripe_customer_id VARCHAR(100),
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- La signup in Supabase Auth se creeaza automat randul din agents
CREATE FUNCTION public.handle_new_user()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = ''
AS $$
BEGIN
    INSERT INTO public.agents (id, email, full_name, company_name, phone)
    VALUES (
        NEW.id,
        NEW.email,
        NEW.raw_user_meta_data ->> 'full_name',
        NEW.raw_user_meta_data ->> 'company_name',
        NEW.raw_user_meta_data ->> 'phone'
    );
    RETURN NEW;
END;
$$;

CREATE TRIGGER on_auth_user_created
    AFTER INSERT ON auth.users
    FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();

-- Agenti AI per tenant
CREATE TABLE ai_agents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES agents(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    persona TEXT,
    tone VARCHAR(50) DEFAULT 'professional',
    language VARCHAR(10) DEFAULT 'ro',
    whatsapp_number VARCHAR(20),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Proprietati cu embedding pentru RAG
CREATE TABLE properties (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES agents(id) ON DELETE CASCADE,
    ai_agent_id UUID REFERENCES ai_agents(id) ON DELETE SET NULL,
    title VARCHAR(300),
    description TEXT,
    price_eur DECIMAL(12,2),
    price_ron DECIMAL(14,2),
    surface_sqm DECIMAL(8,2),
    rooms INT,
    city VARCHAR(100),
    neighborhood VARCHAR(100),
    property_type VARCHAR(50),
    listing_url TEXT,
    images JSONB DEFAULT '[]',
    embedding VECTOR(1536),
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- HNSW, nu ivfflat: ivfflat construit pe tabela goala are recall prost
-- pana la un REINDEX dupa populare. HNSW nu are nevoie de training.
CREATE INDEX ON properties
    USING hnsw (embedding vector_cosine_ops);

-- Conversatii WhatsApp
CREATE TABLE conversations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES agents(id) ON DELETE CASCADE,
    ai_agent_id UUID REFERENCES ai_agents(id) ON DELETE SET NULL,
    contact_phone VARCHAR(20),
    contact_name VARCHAR(200),
    channel VARCHAR(20) DEFAULT 'whatsapp',
    status VARCHAR(20) DEFAULT 'active',
    lead_score INT DEFAULT 0,
    started_at TIMESTAMPTZ DEFAULT NOW(),
    last_message_at TIMESTAMPTZ
);

-- Mesaje per conversatie
CREATE TABLE messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conversation_id UUID NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    role VARCHAR(20) NOT NULL,
    content TEXT NOT NULL,
    tokens_used INT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Leaduri calificate
CREATE TABLE leads (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES agents(id) ON DELETE CASCADE,
    conversation_id UUID REFERENCES conversations(id) ON DELETE SET NULL,
    name VARCHAR(200),
    phone VARCHAR(20),
    email VARCHAR(255),
    budget_min DECIMAL(12,2),
    budget_max DECIMAL(12,2),
    preferred_city VARCHAR(100),
    preferred_type VARCHAR(50),
    notes TEXT,
    status VARCHAR(20) DEFAULT 'new',
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Metrici utilizare per luna
CREATE TABLE usage_metrics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES agents(id) ON DELETE CASCADE,
    month VARCHAR(7) NOT NULL,
    messages_count INT DEFAULT 0,
    tokens_used BIGINT DEFAULT 0,
    leads_generated INT DEFAULT 0,
    UNIQUE(tenant_id, month)
);

-- Indecsi pe FK-uri: RLS filtreaza pe tenant_id la fiecare query
CREATE INDEX ON ai_agents (tenant_id);
CREATE INDEX ON properties (tenant_id);
CREATE INDEX ON conversations (tenant_id);
CREATE INDEX ON messages (conversation_id);
CREATE INDEX ON leads (tenant_id);
CREATE INDEX ON usage_metrics (tenant_id);

-- ============================================================
-- Row Level Security
-- Fara asta, publishable key-ul (care e public) citeste tot.
-- service_role (secret key, folosit de API-ul .NET) ocoleste RLS.
-- ============================================================

ALTER TABLE agents        ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_agents     ENABLE ROW LEVEL SECURITY;
ALTER TABLE properties    ENABLE ROW LEVEL SECURITY;
ALTER TABLE conversations ENABLE ROW LEVEL SECURITY;
ALTER TABLE messages      ENABLE ROW LEVEL SECURITY;
ALTER TABLE leads         ENABLE ROW LEVEL SECURITY;
ALTER TABLE usage_metrics ENABLE ROW LEVEL SECURITY;

-- Fiecare tenant isi vede doar propriul rand din agents.
-- Fara INSERT: randul e creat de trigger. Fara DELETE: se sterge prin auth.users.
CREATE POLICY agents_select_own ON agents
    FOR SELECT TO authenticated
    USING (id = (SELECT auth.uid()));

CREATE POLICY agents_update_own ON agents
    FOR UPDATE TO authenticated
    USING (id = (SELECT auth.uid()))
    WITH CHECK (id = (SELECT auth.uid()));

-- Tabelele cu tenant_id: acces complet, dar doar in propriul tenant
CREATE POLICY ai_agents_own ON ai_agents
    FOR ALL TO authenticated
    USING (tenant_id = (SELECT auth.uid()))
    WITH CHECK (tenant_id = (SELECT auth.uid()));

CREATE POLICY properties_own ON properties
    FOR ALL TO authenticated
    USING (tenant_id = (SELECT auth.uid()))
    WITH CHECK (tenant_id = (SELECT auth.uid()));

CREATE POLICY conversations_own ON conversations
    FOR ALL TO authenticated
    USING (tenant_id = (SELECT auth.uid()))
    WITH CHECK (tenant_id = (SELECT auth.uid()));

CREATE POLICY leads_own ON leads
    FOR ALL TO authenticated
    USING (tenant_id = (SELECT auth.uid()))
    WITH CHECK (tenant_id = (SELECT auth.uid()));

-- Doar citire: metricile sunt scrise de API prin service_role
CREATE POLICY usage_metrics_select_own ON usage_metrics
    FOR SELECT TO authenticated
    USING (tenant_id = (SELECT auth.uid()));

-- messages nu are tenant_id -> se verifica prin conversatia parinte
CREATE POLICY messages_own ON messages
    FOR ALL TO authenticated
    USING (
        EXISTS (
            SELECT 1 FROM conversations c
            WHERE c.id = messages.conversation_id
              AND c.tenant_id = (SELECT auth.uid())
        )
    )
    WITH CHECK (
        EXISTS (
            SELECT 1 FROM conversations c
            WHERE c.id = messages.conversation_id
              AND c.tenant_id = (SELECT auth.uid())
        )
    );
