using System.Security.Cryptography;
using System.Text;

namespace AgentPlatform.Api.Services.Notifications.WebPush;

/// <summary>
/// Criptarea mesajelor Web Push, dupa RFC 8291 (aes128gcm).
/// </summary>
/// <remarks>
/// Serviciile de push ale browserelor (Google, Mozilla, Apple) nu vad continutul:
/// e cifrat cu o cheie derivata din cheia publica a browserului si dintr-un
/// secret pe care numai el il stie. De aceea nu putem doar sa trimitem JSON.
///
/// Pasii de derivare sunt separati de trimitere ca sa poata fi verificati pe
/// vectorul de test din RFC 8291 §5, unde salt-ul si cheia efemera sunt fixate.
/// </remarks>
public static class WebPushCrypto
{
    /// <summary>Lungimea inregistrarii. Un singur bloc: notificarile sunt mici.</summary>
    private const int RecordSize = 4096;

    /// <summary>Un punct necomprimat P-256: 0x04 urmat de X si Y, cate 32 de octeti.</summary>
    private const int PublicKeyBytes = 65;

    private static readonly byte[] KeyInfoPrefix = "WebPush: info"u8.ToArray();
    private static readonly byte[] CekInfo = "Content-Encoding: aes128gcm\0"u8.ToArray();
    private static readonly byte[] NonceInfo = "Content-Encoding: nonce\0"u8.ToArray();

    /// <summary>Cifreaza mesajul pentru un browser anume.</summary>
    /// <param name="payload">Continutul, de obicei JSON.</param>
    /// <param name="userPublicKey">Cheia publica a browserului (p256dh), 65 de octeti.</param>
    /// <param name="userAuth">Secretul de autentificare al browserului, 16 octeti.</param>
    public static byte[] Encrypt(byte[] payload, byte[] userPublicKey, byte[] userAuth)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var sender = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

        return Encrypt(payload, userPublicKey, userAuth, salt, sender);
    }

    /// <summary>
    /// Varianta cu salt si cheie efemera date din afara — folosita de teste, ca
    /// rezultatul sa fie comparabil cu vectorul din standard.
    /// </summary>
    public static byte[] Encrypt(
        byte[] payload,
        byte[] userPublicKey,
        byte[] userAuth,
        byte[] salt,
        ECDiffieHellman sender)
    {
        if (userPublicKey.Length != PublicKeyBytes)
        {
            throw new ArgumentException(
                $"Cheia publica a browserului are {userPublicKey.Length} octeti, nu {PublicKeyBytes}.",
                nameof(userPublicKey));
        }

        using var user = ImportPublicKey(userPublicKey);
        var senderPublicKey = ExportPublicKey(sender);

        // Secretul comun ECDH, brut: derivarea de mai jos e cea din RFC, nu cea
        // implicita a platformei.
        var sharedSecret = sender.DeriveRawSecretAgreement(user.PublicKey);

        // Prima derivare leaga cheia de ambele parti ale schimbului
        var keyInfo = Concat(
            KeyInfoPrefix,
            [0x00],
            userPublicKey,
            senderPublicKey);

        var initialPrk = HKDF.Extract(HashAlgorithmName.SHA256, sharedSecret, userAuth);
        var ikm = HKDF.Expand(HashAlgorithmName.SHA256, initialPrk, 32, keyInfo);

        var prk = HKDF.Extract(HashAlgorithmName.SHA256, ikm, salt);
        var contentKey = HKDF.Expand(HashAlgorithmName.SHA256, prk, 16, CekInfo);
        var nonce = HKDF.Expand(HashAlgorithmName.SHA256, prk, 12, NonceInfo);

        // 0x02 marcheaza ultima inregistrare; fara el receptorul asteapta alta
        var plaintext = Concat(payload, [0x02]);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(contentKey, tag.Length))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        // Antetul aes128gcm: salt, lungimea inregistrarii, cheia publica a expeditorului
        var recordSize = new byte[4];
        BinaryPrimitivesWriteUInt32BigEndian(recordSize, RecordSize);

        return Concat(
            salt,
            recordSize,
            [(byte)senderPublicKey.Length],
            senderPublicKey,
            ciphertext,
            tag);
    }

    public static ECDiffieHellman ImportPublicKey(byte[] uncompressedPoint)
    {
        var parameters = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = uncompressedPoint[1..33],
                Y = uncompressedPoint[33..65],
            },
        };

        return ECDiffieHellman.Create(parameters);
    }

    public static byte[] ExportPublicKey(ECDiffieHellman key)
    {
        var q = key.ExportParameters(false).Q;
        return Concat([0x04], q.X!, q.Y!);
    }

    /// <summary>base64url fara umplutura, singura forma acceptata de Web Push.</summary>
    public static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static byte[] FromBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        // Convert.FromBase64String cere lungime multiplu de 4
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }

    private static void BinaryPrimitivesWriteUInt32BigEndian(byte[] target, uint value)
    {
        target[0] = (byte)(value >> 24);
        target[1] = (byte)(value >> 16);
        target[2] = (byte)(value >> 8);
        target[3] = (byte)value;
    }

    private static byte[] Concat(params byte[][] parts)
    {
        var result = new byte[parts.Sum(part => part.Length)];
        var offset = 0;

        foreach (var part in parts)
        {
            part.CopyTo(result, offset);
            offset += part.Length;
        }

        return result;
    }
}

/// <summary>
/// Antetul VAPID (RFC 8292): dovedeste serviciului de push cine trimite.
/// </summary>
/// <remarks>
/// Fara el, oricine ar afla adresa unui abonament ar putea trimite notificari
/// in numele aplicatiei. Cheile se genereaza o data si raman in configurare.
/// </remarks>
public static class VapidAuth
{
    /// <summary>Tokenul expira in 12 ore; standardul nu permite mai mult de 24.</summary>
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(12);

    public static string BuildAuthorizationHeader(
        string endpoint,
        string subject,
        string publicKey,
        string privateKey)
    {
        var audience = new Uri(endpoint).GetLeftPart(UriPartial.Authority);

        var header = $$"""{"typ":"JWT","alg":"ES256"}""";
        var expires = DateTimeOffset.UtcNow.Add(TokenLifetime).ToUnixTimeSeconds();
        var payload = $$"""{"aud":"{{audience}}","exp":{{expires}},"sub":"{{subject}}"}""";

        var signingInput =
            $"{WebPushCrypto.ToBase64Url(Encoding.UTF8.GetBytes(header))}." +
            $"{WebPushCrypto.ToBase64Url(Encoding.UTF8.GetBytes(payload))}";

        using var ecdsa = ImportPrivateKey(publicKey, privateKey);

        // ES256 cere R||S brut, nu DER: formatul implicit al .NET ar fi respins
        var signature = ecdsa.SignData(
            Encoding.UTF8.GetBytes(signingInput),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        var token = $"{signingInput}.{WebPushCrypto.ToBase64Url(signature)}";

        return $"vapid t={token}, k={publicKey}";
    }

    /// <summary>Genereaza o pereche noua de chei VAPID, in base64url.</summary>
    public static (string PublicKey, string PrivateKey) GenerateKeys()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = ecdsa.ExportParameters(true);

        var publicKey = new byte[65];
        publicKey[0] = 0x04;
        parameters.Q.X!.CopyTo(publicKey, 1);
        parameters.Q.Y!.CopyTo(publicKey, 33);

        return (
            WebPushCrypto.ToBase64Url(publicKey),
            WebPushCrypto.ToBase64Url(parameters.D!));
    }

    private static ECDsa ImportPrivateKey(string publicKey, string privateKey)
    {
        var publicBytes = WebPushCrypto.FromBase64Url(publicKey);
        var privateBytes = WebPushCrypto.FromBase64Url(privateKey);

        return ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = publicBytes[1..33],
                Y = publicBytes[33..65],
            },
            // D poate veni cu zerouri de inceput taiate; P-256 cere fix 32 de octeti
            D = privateBytes.Length == 32
                ? privateBytes
                : new byte[32 - privateBytes.Length].Concat(privateBytes).ToArray(),
        });
    }
}
