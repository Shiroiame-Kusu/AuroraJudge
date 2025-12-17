import Editor from '@monaco-editor/react'

const languageMap: Record<string, string> = {
  cpp: 'cpp',
  c: 'c',
  java: 'java',
  python: 'python',
  javascript: 'javascript',
  go: 'go',
  rust: 'rust',
}

interface CodeEditorProps {
  value: string
  onChange: (value: string) => void
  language: string
  height?: string | number
  readOnly?: boolean
  theme?: 'vs-dark' | 'light'
}

const CodeEditor = ({
  value,
  onChange,
  language,
  height = '400px',
  readOnly = false,
  theme = 'vs-dark',
}: CodeEditorProps) => {
  const monacoLanguage = languageMap[language] || language
  
  return (
    <Editor
      height={height}
      language={monacoLanguage}
      value={value}
      onChange={(v) => onChange(v || '')}
      theme={theme}
      options={{
        readOnly,
        minimap: { enabled: false },
        fontSize: 14,
        lineNumbers: 'on',
        scrollBeyondLastLine: false,
        automaticLayout: true,
        tabSize: 4,
        wordWrap: 'on',
      }}
    />
  )
}

export default CodeEditor
