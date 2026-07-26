/**
 * Codecul G.711 μ-law, in browser.
 *
 * Twilio Media Streams nu accepta alt format: 8 kHz, mono, μ-law, cadre de 20 ms.
 * Browserul da Float32 la 48 kHz, deci conversia trebuie facuta aici — oglinda
 * exacta a lui `G711.cs` din API, ca ce trimitem sa fie bit-identic cu ce
 * trimite Twilio.
 */

const BIAS = 0x84
const CLIP = 32635

/** Exponentul μ-law = floor(log2(i)), cu 0 pentru i = 0. */
const EXPONENT = /* @__PURE__ */ (() => {
  const table = new Uint8Array(256)
  for (let i = 1; i < 256; i++) table[i] = 31 - Math.clz32(i)
  return table
})()

/** Un eșantion PCM 16-bit cu semn → un octet μ-law. */
function encodeSample(sample: number): number {
  // Semnul se ia inainte de a lucra cu magnitudinea
  const sign = sample < 0 ? 0x80 : 0x00
  let magnitude = sample < 0 ? -sample : sample

  if (magnitude > CLIP) magnitude = CLIP
  magnitude += BIAS

  const exponent = EXPONENT[(magnitude >> 7) & 0xff]
  const mantissa = (magnitude >> (exponent + 3)) & 0x0f

  return ~(sign | (exponent << 4) | mantissa) & 0xff
}

/** Un octet μ-law → un eșantion PCM 16-bit cu semn. */
function decodeSample(byte: number): number {
  const value = ~byte & 0xff
  const sign = value & 0x80
  const exponent = (value >> 4) & 0x07
  const mantissa = value & 0x0f

  const magnitude = (((mantissa << 3) + BIAS) << exponent) - BIAS

  return sign ? -magnitude : magnitude
}

/**
 * Float32 din Web Audio (−1…1) → μ-law.
 *
 * Scalarea cu 32767 in loc de 32768 e intentionata: la −1.0 exact, 32768 ar
 * depași int16 si ar da clic audibil in loc de eșantionul maxim.
 */
export function floatToMulaw(samples: Float32Array): Uint8Array {
  const out = new Uint8Array(samples.length)

  for (let i = 0; i < samples.length; i++) {
    const clamped = Math.max(-1, Math.min(1, samples[i]))
    out[i] = encodeSample(Math.round(clamped * 32767))
  }

  return out
}

/** μ-law → Float32, formatul pe care il cere AudioBuffer la redare. */
export function mulawToFloat(bytes: Uint8Array): Float32Array {
  const out = new Float32Array(bytes.length)

  for (let i = 0; i < bytes.length; i++) {
    out[i] = decodeSample(bytes[i]) / 32768
  }

  return out
}

/** Octeti bruti → base64, formatul in care Twilio ambaleaza audio in JSON. */
export function bytesToBase64(bytes: Uint8Array): string {
  let binary = ''
  for (let i = 0; i < bytes.length; i++) binary += String.fromCharCode(bytes[i])
  return btoa(binary)
}

export function base64ToBytes(base64: string): Uint8Array {
  const binary = atob(base64)
  const bytes = new Uint8Array(binary.length)
  for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i)
  return bytes
}
