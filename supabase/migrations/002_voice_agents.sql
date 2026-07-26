-- Agent vocal: apeluri telefonice cu transcriere, AI si sinteza vocala.

-- ── Agenti vocali ───────────────────────────────────────────────────────────
CREATE TABLE voice_agents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES agents(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    -- Numarul Twilio Voice care raspunde. Unic global: doua conturi nu pot
    -- revendica acelasi numar.
    twilio_phone_number VARCHAR(20) UNIQUE,
    voice_name VARCHAR(50) DEFAULT 'ro-RO-AlinaNeural',
    system_prompt TEXT,
    greeting_message TEXT DEFAULT
        'Bună ziua! Sunt asistentul virtual. Cu ce vă pot ajuta astăzi?',
    -- Plasa de siguranta pe cost: fara ea, un apel uitat deschis arde credit
    max_call_duration_seconds INT DEFAULT 300,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- ── Apeluri ─────────────────────────────────────────────────────────────────
CREATE TABLE voice_calls (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES agents(id) ON DELETE CASCADE,
    voice_agent_id UUID REFERENCES voice_agents(id) ON DELETE SET NULL,
    -- CallSid vine de la Twilio si e unic; il folosim ca sa legam webhookul de
    -- conexiunea WebSocket care se deschide separat.
    call_sid VARCHAR(64) NOT NULL UNIQUE,
    stream_sid VARCHAR(64),
    caller_phone VARCHAR(20) NOT NULL,
    caller_name VARCHAR(200),
    status VARCHAR(20) DEFAULT 'ringing',
    duration_seconds INT,
    transcript TEXT,
    lead_qualified BOOLEAN DEFAULT false,
    viewing_scheduled BOOLEAN DEFAULT false,
    viewing_date_time TIMESTAMPTZ,
    sms_sent BOOLEAN DEFAULT false,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    ended_at TIMESTAMPTZ
);

-- ── Replici din apel ────────────────────────────────────────────────────────
CREATE TABLE voice_messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    voice_call_id UUID NOT NULL REFERENCES voice_calls(id) ON DELETE CASCADE,
    role VARCHAR(20) NOT NULL,
    content TEXT NOT NULL,
    audio_duration_ms INT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- ── Indecsi ─────────────────────────────────────────────────────────────────
CREATE INDEX ON voice_agents (tenant_id);
CREATE INDEX ON voice_calls (tenant_id);
CREATE INDEX ON voice_calls (voice_agent_id);
-- Webhookul caută apelul dupa call_sid la fiecare eveniment Twilio
CREATE INDEX ON voice_calls (call_sid);
CREATE INDEX ON voice_messages (voice_call_id);

-- ── RLS ─────────────────────────────────────────────────────────────────────
-- API-ul foloseste service_role si ocolește RLS, dar clientul React nu:
-- fara politici, publishable key-ul ar citi apelurile tuturor.
ALTER TABLE voice_agents   ENABLE ROW LEVEL SECURITY;
ALTER TABLE voice_calls    ENABLE ROW LEVEL SECURITY;
ALTER TABLE voice_messages ENABLE ROW LEVEL SECURITY;

CREATE POLICY voice_agents_own ON voice_agents
    FOR ALL TO authenticated
    USING (tenant_id = (SELECT auth.uid()))
    WITH CHECK (tenant_id = (SELECT auth.uid()));

CREATE POLICY voice_calls_own ON voice_calls
    FOR ALL TO authenticated
    USING (tenant_id = (SELECT auth.uid()))
    WITH CHECK (tenant_id = (SELECT auth.uid()));

-- voice_messages nu are tenant_id -> se verifica prin apelul parinte
CREATE POLICY voice_messages_own ON voice_messages
    FOR ALL TO authenticated
    USING (
        EXISTS (
            SELECT 1 FROM voice_calls vc
            WHERE vc.id = voice_messages.voice_call_id
              AND vc.tenant_id = (SELECT auth.uid())
        )
    )
    WITH CHECK (
        EXISTS (
            SELECT 1 FROM voice_calls vc
            WHERE vc.id = voice_messages.voice_call_id
              AND vc.tenant_id = (SELECT auth.uid())
        )
    );
