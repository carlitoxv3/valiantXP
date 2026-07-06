export interface GiftCardProvider {
  id: string
  name: string
  instructiveUrl?: string
  logoUrl?: string
  isActive: boolean
  campaignId?: string
  stockCount?: number
  availableCount?: number
}

export interface GiftCardImportResult {
  imported: number
  duplicates: number
  invalid?: string[]
}

export interface GiftCardStock {
  count: number
  available: number
}

export interface UserProfile {
  id: string
  email: string
  displayName?: string
  totalPoints: number
}

export interface AdminStats {
  totalUsers: number
  activeProviders: number
  totalCodes: number
  availableCodes: number
}
