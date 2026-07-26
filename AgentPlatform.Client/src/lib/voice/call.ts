import {
  base64ToBytes,
  bytesToBase64,
  floatToMulaw,
  mulawToFloat,
} from '@/lib/voice/mulaw'

/**
 * Un apel vocal purtat din browser, peste exact acelasi protocol pe care il
 * foloseste Twilio Media Streams.
 *
 * Nu e un mod de simulare separat: serverul nu poate deosebi acest apel de unul
 * venit prin telefon, deci ce testezi aici e codul care va rula in producție.
 */

export type CallState =
  | 'idle'
  | 'connecting'
  | 'listening'
  | 'speaking'
  | 'ended'
  | 'error'

type Handlers = {
  onState: (state: CallState) => void
  /** Nivelul microfonului, 0…1, pentru indicatorul vizual. */
  onLevel: (level: number) => void
  onError: (message: string) => void
}

/** Twilio numara in cadre de 20 ms; la 8 kHz asta e 160 de eșantioane. */
const FRAME_SAMPLES = 160

/** Rata pe care o cere Twilio. Orice altceva ar suna accelerat sau incetinit. */
const TARGET_RATE = 8000

/**
 * Cat audio adunam inainte de a-l programa la redare. Cadrele de 20 ms
 * programate individual produc clicuri intre ele; 80 ms e destul de mult ca
 * redarea sa fie continua si destul de puțin ca raspunsul sa para imediat.
 */
const PLAYBACK_CHUNK_MS = 80

/**
 * Cat timp dupa ultimul audio al agentului mai ținem microfonul inchis. Fara
 * marja, coada raspunsului ajunge in microfon si agentul se transcrie pe el
 * insusi — anularea de ecou a browserului nu e suficienta pe boxe.
 */
const MIC_REOPEN_DELAY_S = 0.25

/** Procesorul care scoate eșantioanele din firul audio pe firul principal. */
const RECORDER_WORKLET = `
class RecorderProcessor extends AudioWorkletProcessor {
  process(inputs) {
    const channel = inputs[0] && inputs[0][0]
    // Copie: bufferul primit e reutilizat de motorul audio dupa return
    if (channel) this.port.postMessage(new Float32Array(channel))
    return true
  }
}
registerProcessor('recorder', RecorderProcessor)
`

/**
 * Reeșantionare liniara cu poziție fracționara pastrata intre blocuri.
 *
 * Majoritatea browserelor accepta `new AudioContext({ sampleRate: 8000 })` si
 * atunci pasul e 1, adica o copiere. Cele care ignora cererea dau 44,1 sau
 * 48 kHz, iar fara reeșantionare vocea ar ajunge la server de sase ori mai
 * rapida. Poziția se pastreaza ca sa nu apara discontinuitați la fiecare bloc.
 */
class Resampler {
  private position = 0
  private readonly step: number

  constructor(step: number) {
    this.step = step
  }

  resample(input: Float32Array): Float32Array {
    if (this.step === 1) return input

    const out: number[] = []
    let position = this.position

    while (position < input.length - 1) {
      const index = Math.floor(position)
      const fraction = position - index
      out.push(input[index] * (1 - fraction) + input[index + 1] * fraction)
      position += this.step
    }

    // Ce depaseste blocul curent se scade, ca urmatorul sa continue de acolo
    this.position = position - input.length
    return Float32Array.from(out)
  }
}

export class BrowserVoiceCall {
  private socket: WebSocket | null = null
  private context: AudioContext | null = null
  private stream: MediaStream | null = null
  private worklet: AudioWorkletNode | null = null
  private workletUrl: string | null = null

  private resampler = new Resampler(1)
  private pending: number[] = []
  private sequence = 1
  private readonly streamSid = `MZ${crypto.randomUUID().replace(/-/g, '')}`

  /** Momentul, pe ceasul AudioContext, pana la care e programat audio de redat. */
  private playbackCursor = 0
  private playbackQueue: Uint8Array[] = []
  private playbackTimer: number | null = null

  private state: CallState = 'idle'
  private stopped = false
  private readonly handlers: Handlers

  constructor(handlers: Handlers) {
    this.handlers = handlers
  }

  async start(streamUrl: string, callSid: string): Promise<void> {
    this.setState('connecting')

    try {
      await this.openMicrophone()
    } catch (error) {
      // Refuzul permisiunii nu e o eroare de rețea: merita alt mesaj
      const denied =
        error instanceof DOMException &&
        (error.name === 'NotAllowedError' || error.name === 'SecurityError')

      this.fail(
        denied
          ? 'Browserul nu are acces la microfon. Permite-l din bara de adresă și încearcă din nou.'
          : 'Nu am putut porni microfonul. Verifică dacă altă aplicație îl folosește.',
      )
      return
    }

    await this.openSocket(streamUrl, callSid)
  }

  /** Inchide apelul curat: anunța serverul, apoi elibereaza microfonul. */
  stop(): void {
    if (this.stopped) return
    this.stopped = true

    if (this.socket?.readyState === WebSocket.OPEN) {
      this.send({ event: 'stop', sequenceNumber: String(this.sequence++), streamSid: this.streamSid })
      this.socket.close()
    }

    if (this.playbackTimer !== null) window.clearInterval(this.playbackTimer)
    this.worklet?.disconnect()
    this.stream?.getTracks().forEach((track) => track.stop())
    void this.context?.close()
    if (this.workletUrl) URL.revokeObjectURL(this.workletUrl)

    // O eroare nu devine „incheiat”: mesajul explica de ce apelul nu a pornit
    if (this.state !== 'error') this.setState('ended')
  }

  private async openMicrophone(): Promise<void> {
    this.stream = await navigator.mediaDevices.getUserMedia({
      audio: {
        channelCount: 1,
        echoCancellation: true,
        noiseSuppression: true,
        autoGainControl: true,
      },
    })

    // Cerem 8 kHz direct: unde e respectat, reeșantionarea noastra devine copiere
    this.context = new AudioContext({ sampleRate: TARGET_RATE })
    await this.context.resume()
    this.resampler = new Resampler(this.context.sampleRate / TARGET_RATE)

    this.workletUrl = URL.createObjectURL(
      new Blob([RECORDER_WORKLET], { type: 'application/javascript' }),
    )
    await this.context.audioWorklet.addModule(this.workletUrl)

    this.worklet = new AudioWorkletNode(this.context, 'recorder')
    this.worklet.port.onmessage = (event) =>
      this.onMicrophoneBlock(event.data as Float32Array)

    this.context.createMediaStreamSource(this.stream).connect(this.worklet)

    // Worklet-ul nu produce ieșire, dar fara destinație unele browsere nu il ruleaza
    this.worklet.connect(this.context.destination)
  }

  private openSocket(streamUrl: string, callSid: string): Promise<void> {
    return new Promise((resolve) => {
      const socket = new WebSocket(streamUrl)
      socket.binaryType = 'arraybuffer'
      this.socket = socket

      socket.onopen = () => {
        this.send({ event: 'connected', protocol: 'Call', version: '1.0.0' })
        this.send({
          event: 'start',
          sequenceNumber: String(this.sequence++),
          streamSid: this.streamSid,
          start: {
            streamSid: this.streamSid,
            callSid,
            tracks: ['inbound'],
            mediaFormat: {
              encoding: 'audio/x-mulaw',
              sampleRate: TARGET_RATE,
              channels: 1,
            },
          },
        })

        this.playbackTimer = window.setInterval(
          () => this.flushPlayback(),
          PLAYBACK_CHUNK_MS / 2,
        )

        this.setState('listening')
        resolve()
      }

      socket.onmessage = (event) => this.onServerMessage(event.data)

      socket.onerror = () => {
        // `onerror` nu spune de ce; `onclose` vine imediat dupa cu detalii
        if (this.state === 'connecting') {
          this.fail('Nu am putut deschide conexiunea audio. Verifică dacă API-ul rulează.')
          resolve()
        }
      }

      socket.onclose = () => {
        if (!this.stopped && this.state !== 'error') this.stop()
        resolve()
      }
    })
  }

  private onMicrophoneBlock(block: Float32Array): void {
    if (!this.socket || this.socket.readyState !== WebSocket.OPEN) return

    const resampled = this.resampler.resample(block)

    let sum = 0
    for (let i = 0; i < resampled.length; i++) sum += resampled[i] * resampled[i]
    this.handlers.onLevel(
      resampled.length ? Math.min(1, Math.sqrt(sum / resampled.length) * 8) : 0,
    )

    // Cat timp agentul vorbeste nu trimitem nimic: altfel se aude pe el insusi
    if (this.agentIsSpeaking()) {
      this.pending.length = 0
      return
    }

    for (let i = 0; i < resampled.length; i++) this.pending.push(resampled[i])

    while (this.pending.length >= FRAME_SAMPLES) {
      const frame = Float32Array.from(this.pending.splice(0, FRAME_SAMPLES))

      this.send({
        event: 'media',
        sequenceNumber: String(this.sequence++),
        streamSid: this.streamSid,
        media: {
          track: 'inbound',
          payload: bytesToBase64(floatToMulaw(frame)),
        },
      })
    }
  }

  private onServerMessage(raw: unknown): void {
    if (typeof raw !== 'string') return

    let message: { event?: string; media?: { payload?: string } }
    try {
      message = JSON.parse(raw)
    } catch {
      return
    }

    if (message.event === 'media' && message.media?.payload) {
      this.playbackQueue.push(base64ToBytes(message.media.payload))
    }
  }

  /** Programeaza la redare tot ce s-a adunat, ca un singur bloc continuu. */
  private flushPlayback(): void {
    const context = this.context
    if (!context || this.playbackQueue.length === 0) {
      if (this.state === 'speaking' && !this.agentIsSpeaking()) {
        this.setState('listening')
      }
      return
    }

    const total = this.playbackQueue.reduce((sum, part) => sum + part.length, 0)
    const mulaw = new Uint8Array(total)
    let offset = 0
    for (const part of this.playbackQueue) {
      mulaw.set(part, offset)
      offset += part.length
    }
    this.playbackQueue.length = 0

    const samples = mulawToFloat(mulaw)
    const buffer = context.createBuffer(1, samples.length, TARGET_RATE)
    buffer.getChannelData(0).set(samples)

    const source = context.createBufferSource()
    source.buffer = buffer
    source.connect(context.destination)

    // Mica marja fața de „acum”: programat exact pe currentTime, inceputul se taie
    const startAt = Math.max(this.playbackCursor, context.currentTime + 0.05)
    source.start(startAt)
    this.playbackCursor = startAt + buffer.duration

    this.setState('speaking')
  }

  private agentIsSpeaking(): boolean {
    if (!this.context) return false
    return this.playbackCursor > this.context.currentTime + MIC_REOPEN_DELAY_S
  }

  private send(payload: unknown): void {
    if (this.socket?.readyState === WebSocket.OPEN) {
      this.socket.send(JSON.stringify(payload))
    }
  }

  private setState(state: CallState): void {
    if (this.state === state) return
    this.state = state
    this.handlers.onState(state)
  }

  private fail(message: string): void {
    this.handlers.onError(message)
    this.setState('error')
    this.stop()
  }
}
