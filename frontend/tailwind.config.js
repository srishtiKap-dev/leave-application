export default {
  darkMode: 'media',
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      fontFamily: { sans: ['Inter', 'system-ui', 'sans-serif'] },
      colors: {
        primary: '#4F46E5',
        'primary-dark': '#3730A3',
        success: '#10B981',
        warning: '#F59E0B',
        danger: '#EF4444'
      },
      animation: {
        fadeIn: 'fadeIn 0.2s ease-in-out'
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0', transform: 'translateY(4px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' }
        }
      }
    }
  },
  plugins: []
};
