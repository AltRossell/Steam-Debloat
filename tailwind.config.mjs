/** @type {import('tailwindcss').Config} */
export default {
  content: ['./src/**/*.{astro,html,js,jsx,md,mdx,svelte,ts,tsx,vue}'],
  theme: {
    extend: {
      colors: {
        'steam-blue': '#1B2838',
        'steam-light': '#2A475E',
        'steam-accent': '#66C0F4',
        'apple-gray': '#F5F5F7',
        'apple-dark': '#1D1D1F',
        'apple-blue': '#007AFF',
      },
      fontFamily: {
        'apple': ['Inter', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'Roboto', 'system-ui', 'sans-serif'],
      },
      animation: {
        'float-gentle': 'float-gentle 8s ease-in-out infinite',
        'slide-up-apple': 'slide-up-apple 0.8s cubic-bezier(0.16, 1, 0.3, 1)',
        'fade-in-apple': 'fade-in-apple 1.2s cubic-bezier(0.16, 1, 0.3, 1) forwards',
        'scale-in': 'scale-in 0.6s cubic-bezier(0.16, 1, 0.3, 1) forwards',
      },
      keyframes: {
        'float-gentle': {
          '0%, 100%': { transform: 'translateY(0px)' },
          '50%': { transform: 'translateY(-10px)' }
        },
        'slide-up-apple': {
          '0%': { transform: 'translateY(60px)', opacity: '0' },
          '100%': { transform: 'translateY(0)', opacity: '1' }
        },
        'fade-in-apple': {
          '0%': { opacity: '0', transform: 'translateY(40px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' }
        },
        'scale-in': {
          '0%': { opacity: '0', transform: 'scale(0.8)' },
          '100%': { opacity: '1', transform: 'scale(1)' }
        }
      },
    }
  },
  plugins: [],
}