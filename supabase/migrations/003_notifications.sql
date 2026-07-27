-- Notificari: ce s-a intamplat in cont, cine trebuie sa afle si pe ce canal.
--
-- Trei tabele cu roluri distincte:
--   notifications            -- ce s-a intamplat (istoricul, si sursa clopotelului)
--   notification_preferences -- ce vrea agentul sa afle si pe ce canal
--   push_subscriptions       -- unde trimitem Web Push (un rand per browser)

CREATE TABLE IF NOT EXISTS notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES agents(id) ON DELETE CASCADE,
    -- lead_qualified | conversation_started | weekly_report | usage_threshold |
    -- voice_call_received | voice_lead_qualified | viewing_scheduled |
    -- inactive_agent_message | integration_failure | call_limit_reached
    type VARCHAR(40) NOT NULL,
    title TEXT NOT NULL,
    body TEXT NOT NULL,
    -- info | success | warning | error — decide culoarea si prioritatea vizuala
    severity VARCHAR(10) NOT NULL DEFAULT 'info',
    -- Ruta din aplicatie catre entitatea care a generat notificarea
    link VARCHAR(200),
    read_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Interogarea implicita: ultimele notificari ale unui cont, cele noi primele
CREATE INDEX IF NOT EXISTS idx_notifications_tenant_created
    ON notifications(tenant_id, created_at DESC);

-- Contorul din clopotel numara doar necitite; indexul partial il face constant
CREATE INDEX IF NOT EXISTS idx_notifications_unread
    ON notifications(tenant_id)
    WHERE read_at IS NULL;

CREATE TABLE IF NOT EXISTS notification_preferences (
    tenant_id UUID NOT NULL REFERENCES agents(id) ON DELETE CASCADE,
    type VARCHAR(40) NOT NULL,
    in_app BOOLEAN NOT NULL DEFAULT true,
    push BOOLEAN NOT NULL DEFAULT false,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    -- Cheia compusa garanteaza o singura preferinta per tip per cont
    PRIMARY KEY (tenant_id, type)
);

CREATE TABLE IF NOT EXISTS push_subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES agents(id) ON DELETE CASCADE,
    -- Adresa data de browser. Unica: reabonarea aceluiasi browser inlocuieste
    -- randul vechi in loc sa trimita notificarea de doua ori.
    endpoint TEXT NOT NULL UNIQUE,
    p256dh TEXT NOT NULL,
    auth TEXT NOT NULL,
    user_agent TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_used_at TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_push_subscriptions_tenant
    ON push_subscriptions(tenant_id);

ALTER TABLE notifications ENABLE ROW LEVEL SECURITY;
ALTER TABLE notification_preferences ENABLE ROW LEVEL SECURITY;
ALTER TABLE push_subscriptions ENABLE ROW LEVEL SECURITY;

CREATE POLICY notifications_own ON notifications
    FOR ALL TO authenticated
    USING (tenant_id = (SELECT auth.uid()))
    WITH CHECK (tenant_id = (SELECT auth.uid()));

CREATE POLICY notification_preferences_own ON notification_preferences
    FOR ALL TO authenticated
    USING (tenant_id = (SELECT auth.uid()))
    WITH CHECK (tenant_id = (SELECT auth.uid()));

CREATE POLICY push_subscriptions_own ON push_subscriptions
    FOR ALL TO authenticated
    USING (tenant_id = (SELECT auth.uid()))
    WITH CHECK (tenant_id = (SELECT auth.uid()));
