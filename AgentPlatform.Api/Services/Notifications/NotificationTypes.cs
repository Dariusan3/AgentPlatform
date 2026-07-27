namespace AgentPlatform.Api.Services.Notifications;

/// <summary>
/// Catalogul tipurilor de notificare.
/// </summary>
/// <remarks>
/// Un singur loc pentru identificator, eticheta si valorile implicite: interfața
/// de setari se genereaza din el, iar un tip nou apare automat acolo. Fara
/// catalog, un tip adaugat in cod ar trimite notificari pe care utilizatorul nu
/// le vede in setari si deci nu le poate opri.
/// </remarks>
public static class NotificationTypes
{
    public const string LeadQualified = "lead_qualified";
    public const string ConversationStarted = "conversation_started";
    public const string WeeklyReport = "weekly_report";
    public const string UsageThreshold = "usage_threshold";

    public const string VoiceCallReceived = "voice_call_received";
    public const string VoiceLeadQualified = "voice_lead_qualified";
    public const string ViewingScheduled = "viewing_scheduled";

    public const string InactiveAgentMessage = "inactive_agent_message";
    public const string IntegrationFailure = "integration_failure";
    public const string CallLimitReached = "call_limit_reached";

    public static readonly IReadOnlyList<NotificationTypeInfo> All =
    [
        new(
            LeadQualified,
            "activitate",
            "Lead nou generat",
            "Când un agent califică un contact.",
            InAppByDefault: true,
            PushByDefault: true),
        new(
            ConversationStarted,
            "activitate",
            "Conversație nouă",
            "La primul mesaj de la un număr necunoscut.",
            InAppByDefault: true,
            PushByDefault: false),
        new(
            WeeklyReport,
            "activitate",
            "Raport săptămânal",
            "Sinteza de luni dimineață.",
            InAppByDefault: true,
            PushByDefault: false),
        new(
            UsageThreshold,
            "activitate",
            "Alertă la 80% din limită",
            "Ca să nu te prindă nepregătit la finalul lunii.",
            InAppByDefault: true,
            PushByDefault: false),

        new(
            VoiceCallReceived,
            "apeluri",
            "Apel primit",
            "Când un agent vocal preia un apel.",
            InAppByDefault: true,
            PushByDefault: false),
        new(
            VoiceLeadQualified,
            "apeluri",
            "Lead calificat la telefon",
            "Apelantul a spus ce caută și cât are de cheltuit.",
            InAppByDefault: true,
            PushByDefault: true),
        new(
            ViewingScheduled,
            "apeluri",
            "Vizionare programată",
            "Agentul vocal a stabilit o dată cu clientul.",
            InAppByDefault: true,
            PushByDefault: true),

        new(
            InactiveAgentMessage,
            "operational",
            "Mesaj către un agent oprit",
            "Cineva a scris unui agent care nu răspunde.",
            InAppByDefault: true,
            PushByDefault: false),
        new(
            IntegrationFailure,
            "operational",
            "Eșec la Twilio sau Groq",
            "Un serviciu extern a refuzat cererea.",
            InAppByDefault: true,
            PushByDefault: true),
        new(
            CallLimitReached,
            "operational",
            "Limită de apel atinsă",
            "Un apel a fost închis pentru că a depășit durata maximă.",
            InAppByDefault: true,
            PushByDefault: false),
    ];

    private static readonly Dictionary<string, NotificationTypeInfo> ById =
        All.ToDictionary(info => info.Id);

    public static bool IsKnown(string type) => ById.ContainsKey(type);

    public static NotificationTypeInfo? Find(string type) =>
        ById.GetValueOrDefault(type);
}

/// <param name="Group">activitate | apeluri | operational — gruparea din setari.</param>
public record NotificationTypeInfo(
    string Id,
    string Group,
    string Title,
    string Description,
    bool InAppByDefault,
    bool PushByDefault);
