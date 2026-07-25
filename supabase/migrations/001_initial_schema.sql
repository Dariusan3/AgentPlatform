-- Activare pgvector pentru RAG
CREATE EXTENSION IF NOT EXISTS vector;

-- Tabela principala agenti (tenants)
CREATE TABLE agents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    full_name VARCHAR(200),
    company_name VARCHAR(200),
    phone VARCHAR(20),
    plan VARCHAR(20) DEFAULT 'starter',
    stripe_customer_id VARCHAR(100),
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Agenti AI per tenant
CREATE TABLE ai_agents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES agents(id) ON DELETE CASCADE,
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
    tenant_id UUID REFERENCES agents(id) ON DELETE CASCADE,
    ai_agent_id UUID REFERENCES ai_agents(id),
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

-- Index pentru cautare RAG rapida
CREATE INDEX ON properties
    USING ivfflat (embedding vector_cosine_ops)
    WITH (lists = 100);

-- Conversatii WhatsApp
CREATE TABLE conversations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES agents(id) ON DELETE CASCADE,
    ai_agent_id UUID REFERENCES ai_agents(id),
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
    conversation_id UUID REFERENCES conversations(id) ON DELETE CASCADE,
    role VARCHAR(20) NOT NULL,
    content TEXT NOT NULL,
    tokens_used INT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Leaduri calificate
CREATE TABLE leads (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES agents(id) ON DELETE CASCADE,
    conversation_id UUID REFERENCES conversations(id),
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
    tenant_id UUID REFERENCES agents(id) ON DELETE CASCADE,
    month VARCHAR(7) NOT NULL,
    messages_count INT DEFAULT 0,
    tokens_used BIGINT DEFAULT 0,
    leads_generated INT DEFAULT 0,
    UNIQUE(tenant_id, month)
);