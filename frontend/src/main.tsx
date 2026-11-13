import '@fontsource/dm-sans/latin-400.css';
import '@fontsource/dm-sans/latin-500.css';
import '@fontsource/dm-sans/latin-600.css';
import '@fontsource/dm-sans/latin-700.css';
import React from 'react';
import ReactDOM from 'react-dom/client';
import { App } from './App';
import './index.css';
import './product.css';
try {
  const savedTheme = localStorage.getItem('giatep:theme');
  document.documentElement.dataset.theme = savedTheme === 'dark' || savedTheme === 'light'
    ? savedTheme
    : window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
} catch {
  document.documentElement.dataset.theme = 'light';
}
ReactDOM.createRoot(document.getElementById('root')!).render(<React.StrictMode><App /></React.StrictMode>);
