import { useMutation } from '@tanstack/react-query'
import { api } from '@/lib/api'
import type { ChallengeResult } from '@/types/api'

export const useSubmitChallenge = (challengeId: string) =>
  useMutation<ChallengeResult, Error, { inputs: Record<string, string> }>({
    mutationFn: ({ inputs }) =>
      // Backend expects: { inputs: Record<string,string> } (SubmitChallengeRequestDto)
      api.post(`/dynamics/${challengeId}/submit`, { inputs }).then((r) => r.data),
  })

