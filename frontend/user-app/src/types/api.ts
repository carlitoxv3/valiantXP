export interface UserProfile {
  id: string
  email: string
  displayName?: string
  avatarUrl?: string
  totalPoints: number
  createdAt: string
}

export interface TokenResponse {
  accessToken: string
  refreshToken: string
  user: UserProfile
}

export interface UserIdentity {
  id: string
  provider: 'Google' | 'Spotify' | 'Twitch' | 'EmailOtp' | 'WhatsApp' | 'Telegram'
  externalId: string
  emailClaim?: string
  isEmailVerified: boolean
  isPrimary: boolean
  isActive: boolean
  linkedAt: string
}

export interface Campaign {
  id: string
  name: string
  description?: string
  imageUrl?: string
  startsAt: string
  endsAt: string
  isActive: boolean
  challenges: DynamicChallenge[]
}

export type DynamicType = 'Trivia' | 'Survey' | 'Code' | 'Rally'

export interface DynamicChallenge {
  id: string
  name: string
  description?: string
  type: DynamicType
  configurationJson: string
  isActive: boolean
  campaignId: string
  anonParticipationAllowed?: boolean
}

export interface TriviaConfig {
  questions: TriviaQuestion[]
  timeLimitSeconds?: number
}

export interface TriviaQuestion {
  question: string
  options: { text: string; isCorrect: boolean }[]
  explanation?: string
}

export interface SurveyConfig {
  questions: SurveyQuestion[]
}

export interface SurveyQuestion {
  id: string
  question: string
  type: 'text' | 'rating' | 'multiple_choice'
  options?: string[]
  required: boolean
}

export interface ChallengeResult {
  success: boolean
  message: string
  pointsAwarded?: number
  prize?: UserPrize
  nextChallengeId?: string
}

export interface UserPrize {
  id: string
  prizeId: string
  prizeName: string
  prizeType: 'Points' | 'Product' | 'GiftCard'
  pointsAwarded: number
  giftCardCode?: string
  giftCardRedeemUrl?: string
  awardedAt: string
  isRedeemed: boolean
  expiresAt?: string
}

export interface GuestSession {
  token: string
  expiresAt: string
  status: 'active' | 'converted' | 'expired'
  convertedToUserId?: string
}
