import { useMutation } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { ChallengeResult } from '@/types/api'

export const useSubmitChallenge = (challengeId: string) =>
  useMutation<ChallengeResult, Error, { inputs: Record<string, string> }>({
    mutationFn: ({ inputs }) =>
      api
        .post<ChallengeResult>(`/dynamics/${challengeId}/submit`, { inputs })
        .then((r) => {
          const d = r.data as ChallengeResult & {
            awardedPrizeName?: string
            pointsAwarded?: number
          }
          // Normalize backend response → ChallengeResult
          return {
            ...d,
            pointsAwarded: d.pointsAwarded ?? 0,
            awardedPrizeName: d.awardedPrizeName,
            payload: d.payload,
          } satisfies ChallengeResult
        }),
  })


