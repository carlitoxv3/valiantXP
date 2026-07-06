import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { BrowserRouter } from 'react-router-dom'
import DemoPage from '@/pages/DemoPage'

// DemoPage has:
// - h1 with "ValiantXP" and "Demo Interactiva"
// - Tab buttons: Trivia, Encuesta, Código, Premio, Gift Card, Invitado
// - Default active tab is 'trivia'
// - TriviaChallenge is rendered by default

describe('DemoPage', () => {
  it('renders without crashing', () => {
    render(
      <BrowserRouter>
        <DemoPage />
      </BrowserRouter>
    )
    expect(document.body).not.toBeEmptyDOMElement()
  })

  it('shows the ValiantXP heading', () => {
    render(
      <BrowserRouter>
        <DemoPage />
      </BrowserRouter>
    )
    // h1 contains "ValiantXP" text
    expect(screen.getByText('ValiantXP')).toBeInTheDocument()
  })

  it('shows "Demo Interactiva" subtitle in h1', () => {
    render(
      <BrowserRouter>
        <DemoPage />
      </BrowserRouter>
    )
    expect(screen.getByText('Demo Interactiva')).toBeInTheDocument()
  })

  it('shows trivia tab button', () => {
    render(
      <BrowserRouter>
        <DemoPage />
      </BrowserRouter>
    )
    // Tab label "Trivia"
    expect(screen.getByRole('button', { name: /trivia/i })).toBeInTheDocument()
  })

  it('shows código (code) tab button', () => {
    render(
      <BrowserRouter>
        <DemoPage />
      </BrowserRouter>
    )
    // Tab label "Código"
    expect(screen.getByRole('button', { name: /código/i })).toBeInTheDocument()
  })

  it('shows gift card tab button', () => {
    render(
      <BrowserRouter>
        <DemoPage />
      </BrowserRouter>
    )
    expect(screen.getByRole('button', { name: /gift card/i })).toBeInTheDocument()
  })

  it('shows footer note', () => {
    render(
      <BrowserRouter>
        <DemoPage />
      </BrowserRouter>
    )
    expect(screen.getByText(/ValiantXP Demo/i)).toBeInTheDocument()
  })
})
