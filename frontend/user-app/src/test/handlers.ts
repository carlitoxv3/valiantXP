import { http, HttpResponse } from 'msw'

export const handlers = [
  http.get('http://localhost:5000/api/users/me', () =>
    HttpResponse.json({
      id: 'user-1',
      email: 'test@example.com',
      displayName: 'Test User',
      totalPoints: 150,
      createdAt: new Date().toISOString(),
    })
  ),
  http.get('http://localhost:5000/api/auth/oauth/providers', () =>
    HttpResponse.json([{ provider: 'google' }, { provider: 'spotify' }])
  ),
  http.get('http://localhost:5000/api/users/prizes', () =>
    HttpResponse.json([
      {
        id: 'prize-1',
        prizeId: 'p-1',
        prizeName: '500 Puntos',
        prizeType: 'Points',
        pointsAwarded: 500,
        awardedAt: new Date().toISOString(),
        isRedeemed: false,
      },
      {
        id: 'prize-2',
        prizeId: 'p-2',
        prizeName: 'GiftCard Amazon',
        prizeType: 'GiftCard',
        pointsAwarded: 0,
        giftCardCode: 'AMZN-1234-5678',
        giftCardRedeemUrl: 'https://amazon.com/gc',
        awardedAt: new Date().toISOString(),
        isRedeemed: false,
      },
    ])
  ),
  http.get('http://localhost:5000/api/users/me/identities', () =>
    HttpResponse.json([
      {
        id: 'id-1',
        provider: 'Google',
        externalId: 'google-123',
        emailClaim: 'test@gmail.com',
        isEmailVerified: true,
        isPrimary: true,
        isActive: true,
        linkedAt: new Date().toISOString(),
      },
    ])
  ),
  http.post('http://localhost:5000/api/otp/request', () =>
    HttpResponse.json({ message: 'OTP sent' })
  ),
  http.post('http://localhost:5000/api/otp/verify', () =>
    HttpResponse.json({
      accessToken: 'fake-jwt-token',
      refreshToken: 'fake-refresh',
      user: {
        id: 'user-1',
        email: 'test@example.com',
        displayName: 'Test User',
        totalPoints: 150,
        createdAt: new Date().toISOString(),
      },
    })
  ),
]
