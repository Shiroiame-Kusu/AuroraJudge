/// <reference types="vite/client" />

declare module '*.css' {
  const content: { [className: string]: string };
  export default content;
}

declare module 'katex/dist/katex.min.css';
declare module 'highlight.js/styles/github.css';
