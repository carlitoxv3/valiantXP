import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { OtpInput } from '@/components/auth/OtpInput'

// OtpInput props: value: string[] (array of 6 digits), onChange: (value: string[]) => void
// Renders 6 individual <input> elements (type="text", inputMode="numeric", maxLength=1)

describe('OtpInput', () => {
  it('renders 6 input fields', () => {
    const emptyValue = ['', '', '', '', '', '']
    render(<OtpInput value={emptyValue} onChange={() => {}} />)
    const inputs = screen.getAllByRole('textbox')
    expect(inputs).toHaveLength(6)
  })

  it('each input shows its corresponding digit', () => {
    const value = ['1', '2', '3', '4', '5', '6']
    render(<OtpInput value={value} onChange={() => {}} />)
    const inputs = screen.getAllByRole('textbox') as HTMLInputElement[]
    value.forEach((digit, i) => {
      expect(inputs[i].value).toBe(digit)
    })
  })

  it('calls onChange when a digit is typed', () => {
    const handleChange = vi.fn()
    const emptyValue = ['', '', '', '', '', '']
    render(<OtpInput value={emptyValue} onChange={handleChange} />)
    const inputs = screen.getAllByRole('textbox')
    fireEvent.change(inputs[0], { target: { value: '5' } })
    expect(handleChange).toHaveBeenCalledOnce()
    // Should be called with an array of 6 strings
    const arg = handleChange.mock.calls[0][0] as string[]
    expect(arg).toHaveLength(6)
    expect(arg[0]).toBe('5')
  })

  it('disables all inputs when disabled=true', () => {
    const emptyValue = ['', '', '', '', '', '']
    render(<OtpInput value={emptyValue} onChange={() => {}} disabled />)
    const inputs = screen.getAllByRole('textbox') as HTMLInputElement[]
    inputs.forEach((input) => {
      expect(input).toBeDisabled()
    })
  })

  it('strips non-numeric characters', () => {
    const handleChange = vi.fn()
    const emptyValue = ['', '', '', '', '', '']
    render(<OtpInput value={emptyValue} onChange={handleChange} />)
    const inputs = screen.getAllByRole('textbox')
    fireEvent.change(inputs[0], { target: { value: 'a' } })
    const arg = handleChange.mock.calls[0][0] as string[]
    // 'a' is not a digit, should result in empty string at index 0
    expect(arg[0]).toBe('')
  })
})
