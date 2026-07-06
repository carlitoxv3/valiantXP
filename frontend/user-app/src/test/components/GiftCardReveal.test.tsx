import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { GiftCardReveal } from '@/components/prizes/GiftCardReveal'

// GiftCardReveal props: code: string, redeemUrl?: string, prizeName: string (required)
// States: 'hidden' -> 'revealing' (800ms) -> 'revealed'
// Initial state shows "🎁 Revelar código" button (motion.button)

describe('GiftCardReveal', () => {
  const mockCode = 'AMZN-1234-5678'
  const mockUrl = 'https://amazon.com/gc/redeem'
  const mockPrizeName = 'Gift Card Amazon'

  it('renders prize name', () => {
    render(<GiftCardReveal code={mockCode} redeemUrl={mockUrl} prizeName={mockPrizeName} />)
    expect(screen.getByText(mockPrizeName)).toBeInTheDocument()
  })

  it('renders reveal button initially', () => {
    render(<GiftCardReveal code={mockCode} redeemUrl={mockUrl} prizeName={mockPrizeName} />)
    // The "Revelar código" text is in the motion.button
    expect(screen.getByText(/Revelar código/i)).toBeInTheDocument()
  })

  it('shows code after clicking reveal and waiting for animation', async () => {
    render(<GiftCardReveal code={mockCode} redeemUrl={mockUrl} prizeName={mockPrizeName} />)
    const btn = screen.getByText(/Revelar código/i)
    fireEvent.click(btn)
    // After 800ms setTimeout the state changes to 'revealed'
    await waitFor(
      () => {
        expect(screen.getByText(mockCode)).toBeInTheDocument()
      },
      { timeout: 2000 }
    )
  })

  it('shows redeem URL button after reveal', async () => {
    render(<GiftCardReveal code={mockCode} redeemUrl={mockUrl} prizeName={mockPrizeName} />)
    fireEvent.click(screen.getByText(/Revelar código/i))
    await waitFor(
      () => {
        expect(screen.getByText('Canjear')).toBeInTheDocument()
      },
      { timeout: 2000 }
    )
  })

  it('does not render redeem button when redeemUrl is omitted', async () => {
    render(<GiftCardReveal code={mockCode} prizeName={mockPrizeName} />)
    fireEvent.click(screen.getByText(/Revelar código/i))
    await waitFor(
      () => {
        expect(screen.queryByText('Canjear')).not.toBeInTheDocument()
      },
      { timeout: 2000 }
    )
  })
})
