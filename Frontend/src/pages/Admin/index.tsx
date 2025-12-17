import { useEffect, useMemo, useState } from 'react'
import {
  Card,
  Menu,
  Layout,
  Tabs,
  Table,
  Space,
  Button,
  Input,
  Tag,
  Switch,
  Modal,
  Form,
  message,
  Typography,
  Statistic,
  Row,
  Col,
  Empty,
  Drawer,
  Descriptions,
  Select,
  DatePicker,
  InputNumber,
  Divider,
  Spin,
  Upload,
} from 'antd'
import {
  DashboardOutlined,
  CodeOutlined,
  FileTextOutlined,
  TrophyOutlined,
  UserOutlined,
  SettingOutlined,
  NotificationOutlined,
  AuditOutlined,
  UploadOutlined,
  EyeOutlined,
} from '@ant-design/icons'
import { Routes, Route, useNavigate, useLocation } from 'react-router-dom'
import type { MenuProps } from 'antd'
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table'
import { useRequest } from 'ahooks'
import dayjs from 'dayjs'

import {
  adminService,
  contestService,
  problemService,
  submissionService,
  type AdminUser,
  type RoleDto,
  type PermissionDto,
  type SystemConfigDto,
  type JudgerStatusDto,
  type GenerateJudgerConfigRequest,
  type GenerateJudgerConfigResponse,
  type JudgerRuntimeStatusResponse,
  type AuditLogDto,
  type Problem,
  type ProblemQuery,
  type Tag as ProblemTag,
  type CreateProblemRequest,
  type TestCase,
  type CreateTestCaseRequest,
  type Contest,
  type ContestQuery,
  type CreateContestRequest,
  type UpdateContestRequest,
  ContestStatus,
  ContestType,
  ContestVisibility,
  type Submission,
  type SubmissionQuery,
  type SubmissionDetail,
  type SubmissionSimilarity,
  JudgeStatus,
} from '@/services'

const { Sider, Content } = Layout
const { Text } = Typography

const formatTime = (value?: string | null) => (value ? dayjs(value).format('YYYY-MM-DD HH:mm:ss') : '-')

const Dashboard = () => {
  const { data: usersResp, loading: usersLoading } = useRequest(
    () => adminService.getUsers({ page: 1, pageSize: 1 }),
  )
  const { data: judgersResp, loading: judgersLoading } = useRequest(() => adminService.getJudgers())

  const usersTotal = usersResp?.data?.total ?? 0
  const judgers = judgersResp?.data ?? []
  const onlineJudgers = judgers.filter((j) => j.isOnline).length
  const enabledJudgers = judgers.filter((j) => j.isEnabled).length

  return (
    <Card title="仪表盘">
      <Row gutter={16}>
        <Col xs={24} sm={12} lg={6}>
          <Card bordered={false} loading={usersLoading}>
            <Statistic title="用户总数" value={usersTotal} />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card bordered={false} loading={judgersLoading}>
            <Statistic title="Judger 总数" value={judgers.length} />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card bordered={false} loading={judgersLoading}>
            <Statistic title="在线 Judger" value={onlineJudgers} />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card bordered={false} loading={judgersLoading}>
            <Statistic title="启用 Judger" value={enabledJudgers} />
          </Card>
        </Col>
      </Row>
    </Card>
  )
}

const ProblemManagement = () => {
  const [messageApi, contextHolder] = message.useMessage()
  const [query, setQuery] = useState<ProblemQuery>({ page: 1, pageSize: 20 })
  const [editingId, setEditingId] = useState<string | null>(null)
  const [modalOpen, setModalOpen] = useState(false)
  const [form] = Form.useForm<CreateProblemRequest & { samples?: Array<{ input?: string; output?: string }> }>()

  const [tagModalOpen, setTagModalOpen] = useState(false)
  const [editingTagId, setEditingTagId] = useState<string | null>(null)
  const [tagForm] = Form.useForm<{ name: string; color?: string | null; category?: string | null }>()

  const [tcDrawerOpen, setTcDrawerOpen] = useState(false)
  const [tcProblem, setTcProblem] = useState<{ id: string; title: string } | null>(null)
  const [tcForm] = Form.useForm<CreateTestCaseRequest & { inputFile: any; outputFile: any }>()

  const judgeMode = Form.useWatch('judgeMode', form)

  useEffect(() => {
    if (judgeMode === 1) return
    // When not SPJ, keep SPJ config empty to avoid confusion.
    form.setFieldsValue({
      specialJudgeCode: null,
      specialJudgeLanguage: null,
    } as any)
  }, [judgeMode])

  const { data, loading, refresh } = useRequest(() => problemService.getList(query), { refreshDeps: [query] })
  const { data: tagsResp, refresh: refreshTags } = useRequest(() => problemService.getTags())

  const {
    data: tcResp,
    loading: tcLoading,
    run: loadTestCases,
  } = useRequest((problemId: string) => problemService.getTestCases(problemId), { manual: true })

  const items: Problem[] = data?.data?.items ?? []
  const total = data?.data?.total ?? 0
  const tags: ProblemTag[] = tagsResp?.data ?? []

  const openCreateTag = () => {
    setEditingTagId(null)
    tagForm.resetFields()
    tagForm.setFieldsValue({ name: '', color: null, category: null })
    setTagModalOpen(true)
  }

  const openEditTag = (t: ProblemTag) => {
    setEditingTagId(t.id)
    tagForm.resetFields()
    tagForm.setFieldsValue({ name: t.name, color: t.color ?? null, category: t.category ?? null })
    setTagModalOpen(true)
  }

  const submitTag = async () => {
    const values = await tagForm.validateFields()
    try {
      if (editingTagId) {
        await problemService.updateTag(editingTagId, values)
        messageApi.success('标签已更新')
      } else {
        await problemService.createTag(values)
        messageApi.success('标签已创建')
      }
      setEditingTagId(null)
      tagForm.resetFields()
      tagForm.setFieldsValue({ name: '', color: null, category: null })
      refreshTags()
    } catch (e: any) {
      messageApi.error(e?.message || '保存失败')
    }
  }

  const confirmDeleteTag = (t: ProblemTag) => {
    Modal.confirm({
      title: '删除标签',
      content: `确认删除标签「${t.name}」吗？`,
      okText: '删除',
      okButtonProps: { danger: true },
      cancelText: '取消',
      onOk: async () => {
        try {
          await problemService.deleteTag(t.id)
          messageApi.success('标签已删除')
          refreshTags()
        } catch (e: any) {
          messageApi.error(e?.message || '删除失败')
        }
      },
    })
  }

  const openCreate = () => {
    setEditingId(null)
    form.resetFields()
    setTcProblem(null)
    form.setFieldsValue({
      title: '',
      description: '',
      inputFormat: 'stdin',
      outputFormat: 'stdout',
      timeLimit: 1000,
      memoryLimit: 262144,
      stackLimit: 65536,
      outputLimit: 65536,
      judgeMode: 0,
      visibility: 0,
      difficulty: 0,
      tagIds: [],
      allowedLanguages: null,
      specialJudgeCode: null,
      specialJudgeLanguage: 'cpp',
      sampleInput: null,
      sampleOutput: null,
      samples: [{ input: '', output: '' }],
      hint: null,
      source: null,
    })
    setModalOpen(true)
  }

  const parseSampleField = (text?: string | null): string[] => {
    if (!text) return []
    const raw = String(text).trim()
    if (!raw) return []
    if (raw.startsWith('[')) {
      try {
        const parsed = JSON.parse(raw)
        if (Array.isArray(parsed) && parsed.every((x) => typeof x === 'string')) {
          return parsed.map((s) => String(s))
        }
      } catch {
        // Fall through
      }
    }
    // legacy fallback: allow existing data split by '---'
    return raw
      .split(/\r?\n\s*---\s*\r?\n/g)
      .map((s) => s.trimEnd())
      .filter((s) => s.length > 0)
  }

  const openEdit = async (id: string) => {
    try {
      const resp = await problemService.getById(id)
      const p = resp.data
      if (!p) throw new Error('题目不存在')

      setEditingId(id)
      form.setFieldsValue({
        title: p.title,
        description: p.description,
        inputFormat: p.inputFormat,
        outputFormat: p.outputFormat,
        sampleInput: p.sampleInput ?? null,
        sampleOutput: p.sampleOutput ?? null,
        hint: p.hint ?? null,
        source: p.source ?? null,
        timeLimit: p.timeLimit,
        memoryLimit: p.memoryLimit,
        stackLimit: p.stackLimit,
        outputLimit: p.outputLimit,
        judgeMode: p.judgeMode,
        visibility: p.visibility,
        difficulty: p.difficulty,
        tagIds: (p.tags ?? []).map((t) => t.id),
        allowedLanguages: p.allowedLanguages ?? null,
        specialJudgeCode: p.specialJudgeCode ?? null,
        specialJudgeLanguage: p.specialJudgeLanguage ?? 'cpp',
      })

      const inputs = parseSampleField(p.sampleInput)
      const outputs = parseSampleField(p.sampleOutput)
      const count = Math.max(inputs.length, outputs.length)
      const samples = (count ? Array.from({ length: count }) : [null]).map((_, i) => ({
        input: inputs[i] ?? '',
        output: outputs[i] ?? '',
      }))
      form.setFieldValue('samples', samples)

      // Prepare inline testcase section
      await prepareInlineTestCases(id, p.title)
      setModalOpen(true)
    } catch (e: any) {
      messageApi.error(e?.message || '加载题目失败')
    }
  }

  const prepareInlineTestCases = async (problemId: string, title: string) => {
    setTcProblem({ id: problemId, title })
    tcForm.resetFields()
    tcForm.setFieldsValue({
      order: 1,
      score: 10,
      isSample: false,
      subtask: null,
      description: null,
    } as any)

    try {
      await loadTestCases(problemId)
    } catch (e: any) {
      messageApi.error(e?.message || '加载测试用例失败')
    }
  }

  const openTestCases = async (record: Problem) => {
    setTcProblem({ id: record.id, title: record.title })
    setTcDrawerOpen(true)
    tcForm.resetFields()
    tcForm.setFieldsValue({
      order: 1,
      score: 10,
      isSample: false,
      subtask: null,
      description: null,
    } as any)

    try {
      await loadTestCases(record.id)
    } catch (e: any) {
      messageApi.error(e?.message || '加载测试用例失败')
    }
  }

  const submitTestCase = async () => {
    if (!tcProblem) return
    const values = await tcForm.validateFields()

    const inputFileObj: File | undefined = values.inputFile?.[0]?.originFileObj
    const outputFileObj: File | undefined = values.outputFile?.[0]?.originFileObj

    if (!inputFileObj || !outputFileObj) {
      messageApi.warning('请上传输入/输出文件')
      return
    }

    try {
      await problemService.uploadTestCase(
        tcProblem.id,
        {
          order: values.order,
          score: values.score,
          isSample: values.isSample,
          subtask: values.subtask ?? null,
          description: values.description ?? null,
        },
        inputFileObj,
        outputFileObj
      )
      messageApi.success('测试用例已上传')
      await loadTestCases(tcProblem.id)
    } catch (e: any) {
      messageApi.error(e?.message || '上传失败')
    }
  }

  const deleteTestCase = async (testCaseId: string) => {
    if (!tcProblem) return
    try {
      await problemService.deleteTestCase(tcProblem.id, testCaseId)
      messageApi.success('测试用例已删除')
      await loadTestCases(tcProblem.id)
    } catch (e: any) {
      messageApi.error(e?.message || '删除失败')
    }
  }

  const downloadTestCaseFile = async (testCaseId: string, type: 'input' | 'output') => {
    if (!tcProblem) return
    try {
      const { blob, filename } = await problemService.downloadTestCaseFile(tcProblem.id, testCaseId, type)
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = filename
      document.body.appendChild(a)
      a.click()
      a.remove()
      URL.revokeObjectURL(url)
    } catch (e: any) {
      messageApi.error(e?.message || '下载失败')
    }
  }

  const submit = async () => {
    const values = await form.validateFields()
    try {
      const samples = (values as any).samples as Array<{ input?: string; output?: string }> | undefined
      if (samples && Array.isArray(samples)) {
        const normalized = samples
          .map((s) => ({ input: (s?.input ?? '').trimEnd(), output: (s?.output ?? '').trimEnd() }))
          .filter((s) => s.input.length > 0 || s.output.length > 0)
        const sampleInputArr = normalized.map((s) => s.input)
        const sampleOutputArr = normalized.map((s) => s.output)
        ;(values as any).sampleInput = normalized.length ? JSON.stringify(sampleInputArr) : null
        ;(values as any).sampleOutput = normalized.length ? JSON.stringify(sampleOutputArr) : null
        delete (values as any).samples
      }

      if (editingId) {
        await problemService.update(editingId, values)
        messageApi.success('题目已更新')
      } else {
        await problemService.create(values)
        messageApi.success('题目已创建')
      }
      setModalOpen(false)
      refresh()
    } catch (e: any) {
      messageApi.error(e?.message || '保存失败')
    }
  }

  const confirmDelete = (record: Problem) => {
    Modal.confirm({
      title: '删除题目',
      content: `确认删除题目「${record.title}」吗？`,
      okText: '删除',
      okButtonProps: { danger: true },
      cancelText: '取消',
      onOk: async () => {
        try {
          await problemService.delete(record.id)
          messageApi.success('题目已删除')
          refresh()
        } catch (e: any) {
          messageApi.error(e?.message || '删除失败')
        }
      },
    })
  }

  const columns: ColumnsType<Problem> = [
    {
      title: '题目',
      dataIndex: 'title',
      key: 'title',
    },
    {
      title: '难度',
      dataIndex: 'difficulty',
      key: 'difficulty',
      width: 120,
      render: (d: number) => {
        const map: Record<number, string> = { 0: '未评级', 1: '简单', 2: '中等', 3: '困难', 4: '专家' }
        return <Tag>{map[d] ?? d}</Tag>
      },
    },
    {
      title: '提交/通过',
      key: 'stats',
      width: 120,
      render: (_, r) => `${r.submissionCount} / ${r.acceptedCount}`,
    },
    {
      title: '标签',
      dataIndex: 'tags',
      key: 'tags',
      render: (list: ProblemTag[]) => (
        <Space size={[4, 4]} wrap>
          {(list ?? []).slice(0, 5).map((t) => (
            <Tag key={t.id} color={t.color}>{t.name}</Tag>
          ))}
        </Space>
      ),
    },
    {
      title: '操作',
      key: 'actions',
      width: 160,
      render: (_, record) => (
        <Space>
          <Button size="small" onClick={() => openEdit(record.id)}>编辑</Button>
          <Button size="small" onClick={() => openTestCases(record)}>测试数据</Button>
          <Button size="small" danger onClick={() => confirmDelete(record)}>删除</Button>
        </Space>
      ),
    },
  ]

  const handleTableChange = (pagination: TablePaginationConfig) => {
    setQuery((prev) => ({
      ...prev,
      page: pagination.current ?? 1,
      pageSize: pagination.pageSize ?? 20,
    }))
  }

  return (
    <>
      {contextHolder}
      <Card
        title="题目管理"
        extra={
          <Space>
            <Button type="primary" onClick={openCreate}>创建题目</Button>
            <Button onClick={openCreateTag}>标签管理</Button>
            <Button onClick={refresh}>刷新</Button>
          </Space>
        }
      >
        <Space style={{ marginBottom: 16 }} wrap>
          <Input.Search
            placeholder="搜索题目"
            allowClear
            style={{ width: 260 }}
            onSearch={(value) => setQuery((prev) => ({ ...prev, search: value || undefined, page: 1 }))}
          />
          <Select
            placeholder="难度"
            allowClear
            style={{ width: 160 }}
            onChange={(value) => setQuery((prev) => ({ ...prev, difficulty: value, page: 1 }))}
            options={[
              { value: 0, label: '未评级' },
              { value: 1, label: '简单' },
              { value: 2, label: '中等' },
              { value: 3, label: '困难' },
              { value: 4, label: '专家' },
            ]}
          />
          <Select
            placeholder="标签"
            allowClear
            style={{ width: 240 }}
            onChange={(value) => setQuery((prev) => ({ ...prev, tagId: value, page: 1 }))}
            options={tags.map((t) => ({ value: t.id, label: t.name }))}
          />
        </Space>

        <Table
          rowKey="id"
          columns={columns}
          dataSource={items}
          loading={loading}
          pagination={{
            current: data?.data?.page ?? 1,
            pageSize: data?.data?.pageSize ?? 20,
            total,
            showSizeChanger: true,
            showQuickJumper: true,
            showTotal: (t) => `共 ${t} 道题目`,
          }}
          onChange={handleTableChange}
        />

        <Modal
          title={editingId ? '编辑题目' : '创建题目'}
          open={modalOpen}
          onCancel={() => setModalOpen(false)}
          className="aj-problem-modal"
          style={{ top: 24 }}
          styles={{ body: { maxHeight: 'calc(100vh - 200px)', overflowY: 'auto', overflowX: 'hidden' } }}
          footer={
            <Space>
              <Button onClick={() => setModalOpen(false)}>取消</Button>
              <Button type="primary" onClick={submit}>保存</Button>
            </Space>
          }
          width={900}
        >
          <Form form={form} layout="vertical">
            <Form.Item name="title" label="标题" rules={[{ required: true, message: '请输入标题' }]}>
              <Input />
            </Form.Item>
            <Form.Item name="description" label="题面" rules={[{ required: true, message: '请输入题面' }]}>
              <Input.TextArea rows={6} />
            </Form.Item>
            <Row gutter={16}>
              <Col xs={24} md={12}>
                <Form.Item name="inputFormat" label="输入格式" rules={[{ required: true, message: '请输入输入格式' }]}>
                  <Input.TextArea rows={3} placeholder="stdin" />
                </Form.Item>
              </Col>
              <Col xs={24} md={12}>
                <Form.Item name="outputFormat" label="输出格式" rules={[{ required: true, message: '请输入输出格式' }]}>
                  <Input.TextArea rows={3} placeholder="stdout" />
                </Form.Item>
              </Col>
            </Row>
            <Row gutter={16}>
              <Col xs={24} md={12}>
                <Form.List name="samples">
                  {(fields, { add, remove }) => (
                    <Space direction="vertical" style={{ width: '100%' }}>
                      <Row justify="space-between" align="middle">
                        <Col>
                          <Text>样例（支持多组）</Text>
                        </Col>
                        <Col>
                          <Button size="small" onClick={() => add({ input: '', output: '' })}>
                            添加样例
                          </Button>
                        </Col>
                      </Row>

                      {fields.map((field, idx) => (
                        <Card
                          key={field.key}
                          size="small"
                          title={`样例 ${idx + 1}`}
                          extra={
                            fields.length > 1 ? (
                              <Button size="small" danger onClick={() => remove(field.name)}>
                                删除
                              </Button>
                            ) : null
                          }
                        >
                          <Row gutter={16}>
                            <Col xs={24} md={12}>
                              <Form.Item name={[field.name, 'input']} label="输入">
                                <Input.TextArea rows={3} />
                              </Form.Item>
                            </Col>
                            <Col xs={24} md={12}>
                              <Form.Item name={[field.name, 'output']} label="输出">
                                <Input.TextArea rows={3} />
                              </Form.Item>
                            </Col>
                          </Row>
                        </Card>
                      ))}
                    </Space>
                  )}
                </Form.List>
              </Col>
            </Row>
            <Row gutter={16}>
              <Col xs={24} md={12}>
                <Form.Item name="hint" label="提示">
                  <Input.TextArea rows={2} />
                </Form.Item>
              </Col>
              <Col xs={24} md={12}>
                <Form.Item name="source" label="来源">
                  <Input />
                </Form.Item>
              </Col>
            </Row>

            <Divider />

            <Row gutter={16}>
              <Col xs={24} md={6}>
                <Form.Item name="timeLimit" label="时间限制(ms)" rules={[{ required: true }]}>
                  <InputNumber min={1} style={{ width: '100%' }} />
                </Form.Item>
              </Col>
              <Col xs={24} md={6}>
                <Form.Item name="memoryLimit" label="内存限制(KB)" rules={[{ required: true }]}>
                  <InputNumber min={1} style={{ width: '100%' }} />
                </Form.Item>
              </Col>
              <Col xs={24} md={6}>
                <Form.Item name="stackLimit" label="栈限制(KB)" rules={[{ required: true }]}>
                  <InputNumber min={0} style={{ width: '100%' }} />
                </Form.Item>
              </Col>
              <Col xs={24} md={6}>
                <Form.Item name="outputLimit" label="输出限制(KB)" rules={[{ required: true }]}>
                  <InputNumber min={0} style={{ width: '100%' }} />
                </Form.Item>
              </Col>
            </Row>

            <Row gutter={16}>
              <Col xs={24} md={8}>
                <Form.Item name="judgeMode" label="评测模式" rules={[{ required: true }]}>
                  <Select
                    options={[
                      { value: 0, label: '标准（默认比较输出）' },
                      { value: 1, label: 'TPJ（testlib Checker）' },
                      { value: 2, label: '交互式' },
                      { value: 3, label: '文件比较' },
                    ]}
                  />
                </Form.Item>
              </Col>
              <Col xs={24} md={8}>
                <Form.Item name="visibility" label="可见性" rules={[{ required: true }]}>
                  <Select
                    options={[
                      { value: 0, label: '公开' },
                      { value: 1, label: '私有' },
                      { value: 2, label: '仅比赛可见' },
                      { value: 3, label: '隐藏' },
                    ]}
                  />
                </Form.Item>
              </Col>
              <Col xs={24} md={8}>
                <Form.Item name="difficulty" label="难度" rules={[{ required: true }]}>
                  <Select
                    options={[
                      { value: 0, label: '未评级' },
                      { value: 1, label: '简单' },
                      { value: 2, label: '中等' },
                      { value: 3, label: '困难' },
                      { value: 4, label: '专家' },
                    ]}
                  />
                </Form.Item>
              </Col>
            </Row>

            <Form.Item name="allowedLanguages" label="允许语言" extra="可留空；按后端配置解析（例如 cpp,java,python）">
              <Input />
            </Form.Item>

            {judgeMode === 1 ? (
              <Card size="small" title="TPJ 配置（testlib Checker，退出码 0=AC）" style={{ marginBottom: 16 }}>
                <Form.Item name="specialJudgeLanguage" hidden>
                  <Input />
                </Form.Item>
                <Form.Item name="specialJudgeCode" label="TPJ 代码" rules={[{ required: true, message: '请输入 TPJ 代码' }]}>
                  <Input.TextArea rows={6} placeholder="C++ Checker（可使用 testlib），会以 argv: input.txt expected.txt actual.txt 运行" />
                </Form.Item>
              </Card>
            ) : null}

            <Form.Item name="tagIds" label="标签">
              <Select
                mode="multiple"
                allowClear
                options={tags.map((t) => ({ value: t.id, label: t.name }))}
              />
            </Form.Item>

            <Divider />

            <Card
              size="small"
              title="测试数据"
              extra={
                tcProblem ? (
                  <Space>
                    <Button size="small" onClick={() => loadTestCases(tcProblem.id)} loading={tcLoading}>
                      刷新列表
                    </Button>
                  </Space>
                ) : null
              }
            >
              {!editingId ? (
                <Text type="secondary">请先保存题目后再上传测试数据。</Text>
              ) : (
                <Space direction="vertical" style={{ width: '100%' }} size="large">
                  <Table
                    rowKey="id"
                    size="small"
                    dataSource={(tcResp?.data ?? []) as TestCase[]}
                    pagination={false}
                    loading={tcLoading}
                    columns={[
                      { title: '序号', dataIndex: 'order', key: 'order', width: 80 },
                      { title: '分值', dataIndex: 'score', key: 'score', width: 80 },
                      {
                        title: '样例',
                        dataIndex: 'isSample',
                        key: 'isSample',
                        width: 80,
                        render: (v: boolean) => (v ? <Tag color="green">是</Tag> : <Tag>否</Tag>),
                      },
                      { title: 'Subtask', dataIndex: 'subtask', key: 'subtask', width: 90, render: (v: any) => (v ?? '-') },
                      { title: '输入大小', dataIndex: 'inputSize', key: 'inputSize', width: 110, render: (v: number) => `${(v / 1024).toFixed(1)} KB` },
                      { title: '输出大小', dataIndex: 'outputSize', key: 'outputSize', width: 110, render: (v: number) => `${(v / 1024).toFixed(1)} KB` },
                      { title: '备注', dataIndex: 'description', key: 'description', ellipsis: true },
                      {
                        title: '操作',
                        key: 'actions',
                        width: 220,
                        render: (_: any, r: TestCase) => (
                          <Space>
                            <Button size="small" onClick={() => downloadTestCaseFile(r.id, 'input')}>下载输入</Button>
                            <Button size="small" onClick={() => downloadTestCaseFile(r.id, 'output')}>下载输出</Button>
                            <Button danger size="small" onClick={() => deleteTestCase(r.id)}>删除</Button>
                          </Space>
                        ),
                      },
                    ]}
                  />

                  <Card size="small" title="上传测试用例">
                    <Form form={tcForm} layout="vertical" component="div">
                      <Row gutter={16}>
                        <Col xs={24} md={6}>
                          <Form.Item name="order" label="序号" rules={[{ required: true }]}>
                            <InputNumber min={1} style={{ width: '100%' }} />
                          </Form.Item>
                        </Col>
                        <Col xs={24} md={6}>
                          <Form.Item name="score" label="分值" rules={[{ required: true }]}>
                            <InputNumber min={0} style={{ width: '100%' }} />
                          </Form.Item>
                        </Col>
                        <Col xs={24} md={6}>
                          <Form.Item name="isSample" label="是否样例" valuePropName="checked">
                            <Switch />
                          </Form.Item>
                        </Col>
                        <Col xs={24} md={6}>
                          <Form.Item name="subtask" label="Subtask">
                            <InputNumber min={0} style={{ width: '100%' }} />
                          </Form.Item>
                        </Col>
                      </Row>

                      <Row gutter={16}>
                        <Col xs={24} md={12}>
                          <Form.Item
                            name="inputFile"
                            label="输入文件"
                            valuePropName="fileList"
                            getValueFromEvent={(e) => (Array.isArray(e) ? e : e?.fileList)}
                            rules={[{ required: true, message: '请上传输入文件' }]}
                          >
                            <Upload beforeUpload={() => false} maxCount={1}>
                              <Button icon={<UploadOutlined />}>选择文件</Button>
                            </Upload>
                          </Form.Item>
                        </Col>
                        <Col xs={24} md={12}>
                          <Form.Item
                            name="outputFile"
                            label="输出文件"
                            valuePropName="fileList"
                            getValueFromEvent={(e) => (Array.isArray(e) ? e : e?.fileList)}
                            rules={[{ required: true, message: '请上传输出文件' }]}
                          >
                            <Upload beforeUpload={() => false} maxCount={1}>
                              <Button icon={<UploadOutlined />}>选择文件</Button>
                            </Upload>
                          </Form.Item>
                        </Col>
                      </Row>

                      <Form.Item name="description" label="备注">
                        <Input.TextArea rows={2} />
                      </Form.Item>

                      <Button type="primary" onClick={submitTestCase} disabled={!tcProblem}>
                        上传
                      </Button>
                    </Form>
                  </Card>
                </Space>
              )}
            </Card>
          </Form>
        </Modal>

        <Drawer
          title={tcProblem ? `测试数据 - ${tcProblem.title}` : '测试数据'}
          open={tcDrawerOpen}
          onClose={() => setTcDrawerOpen(false)}
          width={920}
        >
          <Space direction="vertical" style={{ width: '100%' }} size="large">
            <Card size="small" title="测试用例列表" loading={tcLoading}>
              <Table
                rowKey="id"
                size="small"
                dataSource={(tcResp?.data ?? []) as TestCase[]}
                pagination={false}
                columns={[
                  { title: '序号', dataIndex: 'order', key: 'order', width: 80 },
                  { title: '分值', dataIndex: 'score', key: 'score', width: 80 },
                  { title: '样例', dataIndex: 'isSample', key: 'isSample', width: 80, render: (v: boolean) => (v ? <Tag color="green">是</Tag> : <Tag>否</Tag>) },
                  { title: 'Subtask', dataIndex: 'subtask', key: 'subtask', width: 90, render: (v: any) => (v ?? '-') },
                  { title: '输入大小', dataIndex: 'inputSize', key: 'inputSize', width: 110, render: (v: number) => `${(v / 1024).toFixed(1)} KB` },
                  { title: '输出大小', dataIndex: 'outputSize', key: 'outputSize', width: 110, render: (v: number) => `${(v / 1024).toFixed(1)} KB` },
                  { title: '备注', dataIndex: 'description', key: 'description', ellipsis: true },
                  {
                    title: '操作',
                    key: 'actions',
                    width: 220,
                    render: (_: any, r: TestCase) => (
                      <Space>
                        <Button size="small" onClick={() => downloadTestCaseFile(r.id, 'input')}>
                          下载输入
                        </Button>
                        <Button size="small" onClick={() => downloadTestCaseFile(r.id, 'output')}>
                          下载输出
                        </Button>
                        <Button danger size="small" onClick={() => deleteTestCase(r.id)}>
                          删除
                        </Button>
                      </Space>
                    ),
                  },
                ]}
              />
            </Card>

            <Card size="small" title="上传测试用例">
              <Form form={tcForm} layout="vertical" component="div">
                <Row gutter={16}>
                  <Col xs={24} md={6}>
                    <Form.Item name="order" label="序号" rules={[{ required: true }]}>
                      <InputNumber min={1} style={{ width: '100%' }} />
                    </Form.Item>
                  </Col>
                  <Col xs={24} md={6}>
                    <Form.Item name="score" label="分值" rules={[{ required: true }]}>
                      <InputNumber min={0} style={{ width: '100%' }} />
                    </Form.Item>
                  </Col>
                  <Col xs={24} md={6}>
                    <Form.Item name="isSample" label="是否样例" valuePropName="checked">
                      <Switch />
                    </Form.Item>
                  </Col>
                  <Col xs={24} md={6}>
                    <Form.Item name="subtask" label="Subtask">
                      <InputNumber min={0} style={{ width: '100%' }} />
                    </Form.Item>
                  </Col>
                </Row>

                <Row gutter={16}>
                  <Col xs={24} md={12}>
                    <Form.Item
                      name="inputFile"
                      label="输入文件"
                      valuePropName="fileList"
                      getValueFromEvent={(e) => (Array.isArray(e) ? e : e?.fileList)}
                      rules={[{ required: true, message: '请上传输入文件' }]}
                    >
                      <Upload beforeUpload={() => false} maxCount={1}>
                        <Button icon={<UploadOutlined />}>选择文件</Button>
                      </Upload>
                    </Form.Item>
                  </Col>
                  <Col xs={24} md={12}>
                    <Form.Item
                      name="outputFile"
                      label="输出文件"
                      valuePropName="fileList"
                      getValueFromEvent={(e) => (Array.isArray(e) ? e : e?.fileList)}
                      rules={[{ required: true, message: '请上传输出文件' }]}
                    >
                      <Upload beforeUpload={() => false} maxCount={1}>
                        <Button icon={<UploadOutlined />}>选择文件</Button>
                      </Upload>
                    </Form.Item>
                  </Col>
                </Row>

                <Form.Item name="description" label="备注">
                  <Input.TextArea rows={2} />
                </Form.Item>

                <Button type="primary" onClick={submitTestCase} disabled={!tcProblem}>
                  上传
                </Button>
              </Form>
            </Card>
          </Space>
        </Drawer>

        <Modal
          title={editingTagId ? '编辑标签' : '创建标签'}
          open={tagModalOpen}
          onCancel={() => setTagModalOpen(false)}
          onOk={submitTag}
          okText="保存"
          cancelText="取消"
          width={720}
        >
          <Space direction="vertical" style={{ width: '100%' }} size="large">
            <Table
              rowKey="id"
              size="small"
              dataSource={tags}
              pagination={false}
              columns={[
                {
                  title: '名称',
                  dataIndex: 'name',
                  key: 'name',
                  render: (_: any, r: ProblemTag) => <Tag color={r.color}>{r.name}</Tag>,
                },
                { title: '分类', dataIndex: 'category', key: 'category', width: 140, render: (v: any) => v ?? '-' },
                { title: '使用次数', dataIndex: 'usageCount', key: 'usageCount', width: 100 },
                {
                  title: '操作',
                  key: 'actions',
                  width: 160,
                  render: (_: any, r: ProblemTag) => (
                    <Space>
                      <Button size="small" onClick={() => openEditTag(r)}>编辑</Button>
                      <Button danger size="small" onClick={() => confirmDeleteTag(r)}>删除</Button>
                    </Space>
                  ),
                },
              ]}
            />

            <Card size="small" title={editingTagId ? '编辑标签' : '创建标签'}>
              <Form form={tagForm} layout="vertical">
                <Row gutter={16}>
                  <Col xs={24} md={12}>
                    <Form.Item name="name" label="名称" rules={[{ required: true, message: '请输入标签名称' }]}>
                      <Input />
                    </Form.Item>
                  </Col>
                  <Col xs={24} md={12}>
                    <Form.Item name="category" label="分类">
                      <Input placeholder="可选" />
                    </Form.Item>
                  </Col>
                </Row>
                <Form.Item name="color" label="颜色" extra="可选：例如 blue / geekblue / #1677ff">
                  <Input placeholder="可选" />
                </Form.Item>

                <Space>
                  <Button type="primary" onClick={submitTag}>保存</Button>
                  <Button
                    onClick={() => {
                      setEditingTagId(null)
                      tagForm.resetFields()
                      tagForm.setFieldsValue({ name: '', color: null, category: null })
                    }}
                  >
                    新建
                  </Button>
                  <Button onClick={refreshTags}>刷新列表</Button>
                </Space>
              </Form>
            </Card>
          </Space>
        </Modal>
      </Card>
    </>
  )
}

const ContestManagement = () => {
  const [messageApi, contextHolder] = message.useMessage()
  const [query, setQuery] = useState<ContestQuery>({ page: 1, pageSize: 20 })
  const [editingId, setEditingId] = useState<string | null>(null)
  const [modalOpen, setModalOpen] = useState(false)
  const [form] = Form.useForm<Omit<CreateContestRequest, 'startTime' | 'endTime' | 'freezeTime'> & { timeRange: [dayjs.Dayjs, dayjs.Dayjs]; freezeTime?: dayjs.Dayjs | null }>()

  const { data, loading, refresh } = useRequest(() => contestService.getList(query), { refreshDeps: [query] })
  const items: Contest[] = data?.data?.items ?? []
  const total = data?.data?.total ?? 0

  const openCreate = () => {
    setEditingId(null)
    form.resetFields()
    form.setFieldsValue({
      title: '',
      description: null,
      timeRange: [dayjs().add(10, 'minute'), dayjs().add(2, 'hour')],
      freezeTime: null,
      type: ContestType.ACM,
      visibility: ContestVisibility.Public,
      password: null,
      isRated: false,
      ratingFloor: null,
      ratingCeiling: null,
      allowLateSubmission: false,
      lateSubmissionPenalty: 0,
      showRanking: true,
      allowViewOthersCode: false,
      publishProblemsAfterEnd: true,
      maxParticipants: null,
      rules: null,
    })
    setModalOpen(true)
  }

  const openEdit = async (id: string) => {
    try {
      const resp = await contestService.getById(id)
      const c = resp.data
      if (!c) throw new Error('比赛不存在')
      setEditingId(id)
      form.setFieldsValue({
        title: c.title,
        description: c.description ?? null,
        timeRange: [dayjs(c.startTime), dayjs(c.endTime)],
        freezeTime: c.freezeTime ?? null,
        type: c.type,
        visibility: c.visibility,
        password: null,
        isRated: c.isRated,
        ratingFloor: c.ratingFloor ?? null,
        ratingCeiling: c.ratingCeiling ?? null,
        allowLateSubmission: c.allowLateSubmission,
        lateSubmissionPenalty: c.lateSubmissionPenalty,
        showRanking: c.showRanking,
        allowViewOthersCode: c.allowViewOthersCode,
        publishProblemsAfterEnd: c.publishProblemsAfterEnd,
        maxParticipants: c.maxParticipants ?? null,
        rules: c.rules ?? null,
      })
      setModalOpen(true)
    } catch (e: any) {
      messageApi.error(e?.message || '加载比赛失败')
    }
  }

  const submit = async () => {
    const values = await form.validateFields()
    const [start, end] = values.timeRange
    const payloadBase: Omit<CreateContestRequest, 'problems'> & { problems?: any } = {
      title: values.title,
      description: values.description ?? null,
      startTime: start.toISOString(),
      endTime: end.toISOString(),
      freezeTime: values.freezeTime ? dayjs(values.freezeTime).toISOString() : null,
      type: values.type,
      visibility: values.visibility,
      password: values.password ?? null,
      isRated: values.isRated,
      ratingFloor: values.ratingFloor ?? null,
      ratingCeiling: values.ratingCeiling ?? null,
      allowLateSubmission: values.allowLateSubmission,
      lateSubmissionPenalty: values.lateSubmissionPenalty,
      showRanking: values.showRanking,
      allowViewOthersCode: values.allowViewOthersCode,
      publishProblemsAfterEnd: values.publishProblemsAfterEnd,
      maxParticipants: values.maxParticipants ?? null,
      rules: values.rules ?? null,
    }

    try {
      if (editingId) {
        const payload: UpdateContestRequest = payloadBase as UpdateContestRequest
        await contestService.update(editingId, payload)
        messageApi.success('比赛已更新')
      } else {
        const payload: CreateContestRequest = {
          ...(payloadBase as any),
          problems: null,
        }
        await contestService.create(payload)
        messageApi.success('比赛已创建')
      }
      setModalOpen(false)
      refresh()
    } catch (e: any) {
      messageApi.error(e?.message || '保存失败')
    }
  }

  const confirmDelete = (record: Contest) => {
    Modal.confirm({
      title: '删除比赛',
      content: `确认删除比赛「${record.title}」吗？`,
      okText: '删除',
      okButtonProps: { danger: true },
      cancelText: '取消',
      onOk: async () => {
        try {
          await contestService.delete(record.id)
          messageApi.success('比赛已删除')
          refresh()
        } catch (e: any) {
          messageApi.error(e?.message || '删除失败')
        }
      },
    })
  }

  const columns: ColumnsType<Contest> = [
    { title: '名称', dataIndex: 'title', key: 'title' },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 110,
      render: (s: number) => {
        const map: Record<number, { text: string; color: string }> = {
          [ContestStatus.Pending]: { text: '未开始', color: 'default' },
          [ContestStatus.Running]: { text: '进行中', color: 'green' },
          [ContestStatus.Frozen]: { text: '已封榜', color: 'blue' },
          [ContestStatus.Ended]: { text: '已结束', color: 'red' },
        }
        const cfg = map[s]
        return <Tag color={cfg?.color}>{cfg?.text ?? s}</Tag>
      },
    },
    {
      title: '开始',
      dataIndex: 'startTime',
      key: 'startTime',
      width: 170,
      render: (t: string) => dayjs(t).format('YYYY-MM-DD HH:mm'),
    },
    {
      title: '结束',
      dataIndex: 'endTime',
      key: 'endTime',
      width: 170,
      render: (t: string) => dayjs(t).format('YYYY-MM-DD HH:mm'),
    },
    {
      title: '操作',
      key: 'actions',
      width: 160,
      render: (_, record) => (
        <Space>
          <Button size="small" onClick={() => openEdit(record.id)}>编辑</Button>
          <Button size="small" danger onClick={() => confirmDelete(record)}>删除</Button>
        </Space>
      ),
    },
  ]

  const handleTableChange = (pagination: TablePaginationConfig) => {
    setQuery((prev) => ({
      ...prev,
      page: pagination.current ?? 1,
      pageSize: pagination.pageSize ?? 20,
    }))
  }

  return (
    <>
      {contextHolder}
      <Card
        title="比赛管理"
        extra={
          <Space>
            <Button type="primary" onClick={openCreate}>创建比赛</Button>
            <Button onClick={refresh}>刷新</Button>
          </Space>
        }
      >
        <Space style={{ marginBottom: 16 }} wrap>
          <Select
            placeholder="状态"
            allowClear
            style={{ width: 140 }}
            onChange={(value) => setQuery((prev) => ({ ...prev, status: value, page: 1 }))}
            options={[
              { value: ContestStatus.Pending, label: '未开始' },
              { value: ContestStatus.Running, label: '进行中' },
              { value: ContestStatus.Frozen, label: '已封榜' },
              { value: ContestStatus.Ended, label: '已结束' },
            ]}
          />
          <Select
            placeholder="类型"
            allowClear
            style={{ width: 140 }}
            onChange={(value) => setQuery((prev) => ({ ...prev, type: value, page: 1 }))}
            options={[
              { value: ContestType.ACM, label: 'ACM' },
              { value: ContestType.OI, label: 'OI' },
              { value: ContestType.IOI, label: 'IOI' },
              { value: ContestType.LeDuo, label: 'LeDuo' },
              { value: ContestType.Homework, label: '作业' },
            ]}
          />
        </Space>

        <Table
          rowKey="id"
          columns={columns}
          dataSource={items}
          loading={loading}
          pagination={{
            current: data?.data?.page ?? 1,
            pageSize: data?.data?.pageSize ?? 20,
            total,
            showSizeChanger: true,
            showQuickJumper: true,
            showTotal: (t) => `共 ${t} 场比赛`,
          }}
          onChange={handleTableChange}
        />

        <Modal
          title={editingId ? '编辑比赛' : '创建比赛'}
          open={modalOpen}
          onCancel={() => setModalOpen(false)}
          onOk={submit}
          okText="保存"
          cancelText="取消"
          width={900}
        >
          <Form form={form} layout="vertical">
            <Form.Item name="title" label="标题" rules={[{ required: true, message: '请输入标题' }]}>
              <Input />
            </Form.Item>
            <Form.Item name="description" label="简介">
              <Input.TextArea rows={3} />
            </Form.Item>

            <Form.Item
              name="timeRange"
              label="开始/结束"
              rules={[{ required: true, message: '请选择开始/结束时间' }]}
            >
              <DatePicker.RangePicker showTime style={{ width: '100%' }} />
            </Form.Item>

            <Row gutter={16}>
              <Col xs={24} md={8}>
                <Form.Item name="freezeTime" label="封榜时间">
                  <DatePicker showTime style={{ width: '100%' }} />
                </Form.Item>
              </Col>
              <Col xs={24} md={8}>
                <Form.Item name="type" label="类型" rules={[{ required: true }]}>
                  <Select
                    options={[
                      { value: ContestType.ACM, label: 'ACM' },
                      { value: ContestType.OI, label: 'OI' },
                      { value: ContestType.IOI, label: 'IOI' },
                      { value: ContestType.LeDuo, label: 'LeDuo' },
                      { value: ContestType.Homework, label: '作业' },
                    ]}
                  />
                </Form.Item>
              </Col>
              <Col xs={24} md={8}>
                <Form.Item name="visibility" label="可见性" rules={[{ required: true }]}>
                  <Select
                    options={[
                      { value: ContestVisibility.Public, label: '公开' },
                      { value: ContestVisibility.Protected, label: '受保护(需要密码)' },
                      { value: ContestVisibility.Private, label: '私有' },
                    ]}
                  />
                </Form.Item>
              </Col>
            </Row>

            <Row gutter={16}>
              <Col xs={24} md={12}>
                <Form.Item name="password" label="密码" extra="仅当可见性为受保护时需要">
                  <Input.Password />
                </Form.Item>
              </Col>
              <Col xs={24} md={12}>
                <Form.Item name="maxParticipants" label="最大参与人数">
                  <InputNumber min={0} style={{ width: '100%' }} />
                </Form.Item>
              </Col>
            </Row>

            <Row gutter={16}>
              <Col xs={24} md={8}>
                <Form.Item name="isRated" label="是否计分" valuePropName="checked">
                  <Switch />
                </Form.Item>
              </Col>
              <Col xs={24} md={8}>
                <Form.Item name="ratingFloor" label="Rating 下限">
                  <InputNumber style={{ width: '100%' }} />
                </Form.Item>
              </Col>
              <Col xs={24} md={8}>
                <Form.Item name="ratingCeiling" label="Rating 上限">
                  <InputNumber style={{ width: '100%' }} />
                </Form.Item>
              </Col>
            </Row>

            <Row gutter={16}>
              <Col xs={24} md={8}>
                <Form.Item name="allowLateSubmission" label="允许赛后提交" valuePropName="checked">
                  <Switch />
                </Form.Item>
              </Col>
              <Col xs={24} md={8}>
                <Form.Item name="lateSubmissionPenalty" label="赛后提交惩罚">
                  <InputNumber min={0} style={{ width: '100%' }} />
                </Form.Item>
              </Col>
              <Col xs={24} md={8}>
                <Form.Item name="showRanking" label="显示排行榜" valuePropName="checked">
                  <Switch />
                </Form.Item>
              </Col>
            </Row>

            <Row gutter={16}>
              <Col xs={24} md={8}>
                <Form.Item name="allowViewOthersCode" label="允许查看他人代码" valuePropName="checked">
                  <Switch />
                </Form.Item>
              </Col>
              <Col xs={24} md={8}>
                <Form.Item name="publishProblemsAfterEnd" label="结束后公开题目" valuePropName="checked">
                  <Switch />
                </Form.Item>
              </Col>
            </Row>

            <Form.Item name="rules" label="规则/说明">
              <Input.TextArea rows={4} />
            </Form.Item>
          </Form>
        </Modal>
      </Card>
    </>
  )
}

const SubmissionManagement = () => {
  const [messageApi, contextHolder] = message.useMessage()
  const [query, setQuery] = useState<SubmissionQuery>({ page: 1, pageSize: 20 })
  const [detailOpen, setDetailOpen] = useState(false)
  const [detailLoading, setDetailLoading] = useState(false)
  const [detail, setDetail] = useState<SubmissionDetail | null>(null)
  const [similarityOpen, setSimilarityOpen] = useState(false)
  const [similarities, setSimilarities] = useState<SubmissionSimilarity[]>([])
  const [similarityLoading, setSimilarityLoading] = useState(false)

  const { data, loading, refresh } = useRequest(() => submissionService.getList(query), { refreshDeps: [query] })
  const items: Submission[] = data?.data?.items ?? []
  const total = data?.data?.total ?? 0

  const openDetail = async (id: string) => {
    setDetailOpen(true)
    setDetailLoading(true)
    try {
      const resp = await submissionService.getById(id)
      if (!resp.data) throw new Error('提交不存在')
      setDetail(resp.data)
    } catch (e: any) {
      messageApi.error(e?.message || '加载提交失败')
      setDetail(null)
    } finally {
      setDetailLoading(false)
    }
  }

  const doRejudge = async (id: string) => {
    try {
      await submissionService.rejudge(id)
      messageApi.success('已发起重测')
      refresh()
      if (detail?.id === id) {
        openDetail(id)
      }
    } catch (e: any) {
      messageApi.error(e?.message || '重测失败')
    }
  }

  const openSimilarity = async (id: string) => {
    setSimilarityOpen(true)
    setSimilarityLoading(true)
    try {
      const resp = await submissionService.getSimilarity(id, { top: 10, candidateLimit: 500 })
      setSimilarities(resp.data ?? [])
    } catch (e: any) {
      messageApi.error(e?.message || '相似度检测失败')
      setSimilarities([])
    } finally {
      setSimilarityLoading(false)
    }
  }

  const columns: ColumnsType<Submission> = [
    {
      title: '时间',
      dataIndex: 'submittedAt',
      key: 'submittedAt',
      width: 180,
      render: (t: string) => dayjs(t).format('YYYY-MM-DD HH:mm:ss'),
    },
    { title: '题目', dataIndex: 'problemTitle', key: 'problemTitle' },
    { title: '用户', dataIndex: 'username', key: 'username', width: 140 },
    { title: '语言', dataIndex: 'language', key: 'language', width: 110 },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 140,
      render: (s: number) => <Tag>{s}</Tag>,
    },
    {
      title: '操作',
      key: 'actions',
      width: 240,
      render: (_, record) => (
        <Space>
          <Button size="small" onClick={() => openDetail(record.id)}>查看</Button>
          <Button size="small" onClick={() => doRejudge(record.id)}>重测</Button>
          <Button size="small" onClick={() => openSimilarity(record.id)}>相似度</Button>
        </Space>
      ),
    },
  ]

  const handleTableChange = (pagination: TablePaginationConfig) => {
    setQuery((prev) => ({
      ...prev,
      page: pagination.current ?? 1,
      pageSize: pagination.pageSize ?? 20,
    }))
  }

  const similarityColumns: ColumnsType<SubmissionSimilarity> = [
    { title: '相似度(%)', dataIndex: 'similarity', key: 'similarity', width: 120 },
    { title: '用户', dataIndex: 'username', key: 'username', width: 160 },
    { title: '提交时间', dataIndex: 'submittedAt', key: 'submittedAt', width: 180, render: (t: string) => dayjs(t).format('YYYY-MM-DD HH:mm:ss') },
    { title: '代码长度', dataIndex: 'codeLength', key: 'codeLength', width: 100 },
    {
      title: '操作',
      key: 'actions',
      width: 100,
      render: (_, r) => <Button size="small" onClick={() => openDetail(r.submissionId)}>查看</Button>,
    },
  ]

  return (
    <>
      {contextHolder}
      <Card
        title="提交管理"
        extra={<Button onClick={refresh}>刷新</Button>}
      >
        <Space style={{ marginBottom: 16 }} wrap>
          <Select
            placeholder="语言"
            allowClear
            style={{ width: 140 }}
            onChange={(value) => setQuery((prev) => ({ ...prev, language: value, page: 1 }))}
            options={[
              { value: 'cpp', label: 'C++' },
              { value: 'c', label: 'C' },
              { value: 'java', label: 'Java' },
              { value: 'python', label: 'Python' },
            ]}
          />
          <Select
            placeholder="状态"
            allowClear
            style={{ width: 180 }}
            onChange={(value) => setQuery((prev) => ({ ...prev, status: value, page: 1 }))}
            options={[
              { value: JudgeStatus.Accepted, label: '通过' },
              { value: JudgeStatus.WrongAnswer, label: '答案错误' },
              { value: JudgeStatus.TimeLimitExceeded, label: '超时' },
              { value: JudgeStatus.MemoryLimitExceeded, label: '超内存' },
              { value: JudgeStatus.RuntimeError, label: '运行错误' },
              { value: JudgeStatus.CompileError, label: '编译错误' },
            ]}
          />
        </Space>

        <Table
          rowKey="id"
          columns={columns}
          dataSource={items}
          loading={loading}
          pagination={{
            current: data?.data?.page ?? 1,
            pageSize: data?.data?.pageSize ?? 20,
            total,
            showSizeChanger: true,
            showQuickJumper: true,
            showTotal: (t) => `共 ${t} 条记录`,
          }}
          onChange={handleTableChange}
        />
      </Card>

      <Drawer
        title="提交详情"
        open={detailOpen}
        onClose={() => setDetailOpen(false)}
        width={900}
      >
        {detailLoading || !detail ? (
          <Card loading />
        ) : (
          <>
            <Space style={{ marginBottom: 12 }}>
              <Button onClick={() => doRejudge(detail.id)}>重测</Button>
              <Button onClick={() => openSimilarity(detail.id)}>相似度</Button>
            </Space>
            <Descriptions bordered size="small" column={2}>
              <Descriptions.Item label="题目">{detail.problemTitle}</Descriptions.Item>
              <Descriptions.Item label="用户">{detail.username}</Descriptions.Item>
              <Descriptions.Item label="语言">{detail.language}</Descriptions.Item>
              <Descriptions.Item label="状态">{detail.status}</Descriptions.Item>
              <Descriptions.Item label="提交时间">{dayjs(detail.submittedAt).format('YYYY-MM-DD HH:mm:ss')}</Descriptions.Item>
              <Descriptions.Item label="评测时间">{detail.judgedAt ? dayjs(detail.judgedAt).format('YYYY-MM-DD HH:mm:ss') : '-'}</Descriptions.Item>
            </Descriptions>

            <Divider />
            <Typography.Title level={5}>代码</Typography.Title>
            <pre style={{ whiteSpace: 'pre-wrap' }}>{detail.code}</pre>

            {detail.compileMessage ? (
              <>
                <Divider />
                <Typography.Title level={5}>编译信息</Typography.Title>
                <pre style={{ whiteSpace: 'pre-wrap' }}>{detail.compileMessage}</pre>
              </>
            ) : null}

            {detail.judgeMessage ? (
              <>
                <Divider />
                <Typography.Title level={5}>评测信息</Typography.Title>
                <pre style={{ whiteSpace: 'pre-wrap' }}>{detail.judgeMessage}</pre>
              </>
            ) : null}

            <Divider />
            <Typography.Title level={5}>测试点结果</Typography.Title>
            <Table
              rowKey={(r: any) => `${r.testCaseOrder}`}
              size="small"
              pagination={false}
              columns={[
                { title: '#', dataIndex: 'testCaseOrder', key: 'testCaseOrder', width: 70 },
                { title: '状态', dataIndex: 'status', key: 'status', width: 100, render: (s: number) => <Tag>{s}</Tag> },
                { title: '用时', dataIndex: 'timeUsed', key: 'timeUsed', width: 90, render: (v: number) => `${v} ms` },
                { title: '内存', dataIndex: 'memoryUsed', key: 'memoryUsed', width: 100, render: (v: number) => `${v} KB` },
                { title: '分数', dataIndex: 'score', key: 'score', width: 80 },
                { title: '信息', dataIndex: 'message', key: 'message' },
              ]}
              dataSource={(detail.results ?? []) as any}
            />
          </>
        )}
      </Drawer>

      <Modal
        title="相似度检测"
        open={similarityOpen}
        onCancel={() => setSimilarityOpen(false)}
        footer={null}
        width={800}
      >
        <Table
          rowKey="submissionId"
          loading={similarityLoading}
          columns={similarityColumns}
          dataSource={similarities}
          pagination={false}
        />
      </Modal>
    </>
  )
}

const UserManagement = () => {
  const [query, setQuery] = useState<{ page: number; pageSize: number; search?: string }>({
    page: 1,
    pageSize: 20,
  })
  const [messageApi, contextHolder] = message.useMessage()

  const { data: usersResp, loading: usersLoading, refresh: refreshUsers } = useRequest(
    () => adminService.getUsers(query),
    { refreshDeps: [query] }
  )
  const { data: rolesResp, loading: rolesLoading, refresh: refreshRoles } = useRequest(() => adminService.getRoles())
  const { data: permissionsResp, loading: permissionsLoading } = useRequest(() => adminService.getPermissions())

  const users = usersResp?.data?.items ?? []
  const usersTotal = usersResp?.data?.total ?? 0

  const roles = rolesResp?.data ?? []
  const permissions = permissionsResp?.data ?? []

  const [roleModalOpen, setRoleModalOpen] = useState(false)
  const [editingRole, setEditingRole] = useState<RoleDto | null>(null)
  const [roleForm] = Form.useForm()

  const handleBanToggle = async (user: AdminUser) => {
    try {
      // status enum: Active is typically 0/1; backend uses UserStatus enum, ban endpoint toggles.
      // Here we treat non-active as banned.
      if (user.status === 0 || user.status === 1) {
        await adminService.banUser(user.id)
        messageApi.success('用户已禁用')
      } else {
        await adminService.unbanUser(user.id)
        messageApi.success('用户已解禁')
      }
      refreshUsers()
    } catch (e: any) {
      messageApi.error(e?.message || '操作失败')
    }
  }

  const userColumns: ColumnsType<AdminUser> = useMemo(() => [
    {
      title: '用户名',
      dataIndex: 'username',
      key: 'username',
    },
    {
      title: '邮箱',
      dataIndex: 'email',
      key: 'email',
    },
    {
      title: '角色',
      dataIndex: 'roles',
      key: 'roles',
      render: (value?: string[]) => {
        const list = value ?? []
        if (!list.length) return <Text type="secondary">-</Text>
        return (
          <Space size={[4, 4]} wrap>
            {list.map((r) => (
              <Tag key={r}>{r}</Tag>
            ))}
          </Space>
        )
      },
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      render: (status: number) => {
        // UserStatus enum lives on backend; we keep a simple visual.
        // 0/1 treated as Active, other values treated as Disabled.
        const active = status === 0 || status === 1
        return <Tag color={active ? 'green' : 'red'}>{active ? '正常' : '禁用'}</Tag>
      },
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (value: string) => formatTime(value),
    },
    {
      title: '最后登录',
      dataIndex: 'lastLoginAt',
      key: 'lastLoginAt',
      render: (value?: string | null) => formatTime(value),
    },
    {
      title: '操作',
      key: 'actions',
      render: (_, record) => (
        <Button size="small" onClick={() => handleBanToggle(record)}>
          {(record.status === 0 || record.status === 1) ? '禁用' : '解禁'}
        </Button>
      ),
    },
  ], [messageApi])

  const handleUserTableChange = (pagination: TablePaginationConfig) => {
    setQuery((prev) => ({
      ...prev,
      page: pagination.current || 1,
      pageSize: pagination.pageSize || 20,
    }))
  }

  const openCreateRole = () => {
    setEditingRole(null)
    roleForm.resetFields()
    setRoleModalOpen(true)
  }

  const openEditRole = (role: RoleDto) => {
    setEditingRole(role)
    roleForm.setFieldsValue({
      name: role.name,
      code: role.code,
      description: role.description ?? '',
      priority: role.priority,
    })
    setRoleModalOpen(true)
  }

  const submitRole = async () => {
    const values = await roleForm.validateFields()
    try {
      if (editingRole) {
        await adminService.updateRole(editingRole.id, values)
        messageApi.success('角色已更新')
      } else {
        await adminService.createRole(values)
        messageApi.success('角色已创建')
      }
      setRoleModalOpen(false)
      refreshRoles()
    } catch (e: any) {
      messageApi.error(e?.message || '保存失败')
    }
  }

  const deleteRole = async (role: RoleDto) => {
    Modal.confirm({
      title: '删除角色',
      content: `确认删除角色 ${role.name} 吗？`,
      okText: '删除',
      okButtonProps: { danger: true },
      cancelText: '取消',
      onOk: async () => {
        try {
          await adminService.deleteRole(role.id)
          messageApi.success('角色已删除')
          refreshRoles()
        } catch (e: any) {
          messageApi.error(e?.message || '删除失败')
        }
      },
    })
  }

  const roleColumns: ColumnsType<RoleDto> = [
    { title: '名称', dataIndex: 'name', key: 'name' },
    { title: '代码', dataIndex: 'code', key: 'code' },
    { title: '优先级', dataIndex: 'priority', key: 'priority' },
    {
      title: '系统角色',
      dataIndex: 'isSystem',
      key: 'isSystem',
      render: (v: boolean) => (v ? <Tag color="blue">是</Tag> : <Tag>否</Tag>),
    },
    {
      title: '操作',
      key: 'actions',
      render: (_, record) => (
        <Space>
          <Button size="small" onClick={() => openEditRole(record)} disabled={record.isSystem}>
            编辑
          </Button>
          <Button size="small" danger onClick={() => deleteRole(record)} disabled={record.isSystem}>
            删除
          </Button>
        </Space>
      ),
    },
  ]

  const permissionColumns: ColumnsType<PermissionDto> = [
    { title: '名称', dataIndex: 'name', key: 'name' },
    { title: '代码', dataIndex: 'code', key: 'code' },
    { title: '分类', dataIndex: 'category', key: 'category' },
    { title: '排序', dataIndex: 'order', key: 'order' },
  ]

  return (
    <>
      {contextHolder}
      <Card title="用户管理">
        <Tabs
          items={[
            {
              key: 'users',
              label: '用户',
              children: (
                <>
                  <Space style={{ marginBottom: 16 }}>
                    <Input.Search
                      placeholder="搜索用户名/邮箱"
                      allowClear
                      onSearch={(value) => setQuery((prev) => ({ ...prev, search: value || undefined, page: 1 }))}
                      style={{ width: 260 }}
                    />
                    <Button onClick={refreshUsers}>刷新</Button>
                  </Space>
                  <Table
                    rowKey="id"
                    columns={userColumns}
                    dataSource={users}
                    loading={usersLoading}
                    pagination={{
                      current: usersResp?.data?.page || 1,
                      pageSize: usersResp?.data?.pageSize || 20,
                      total: usersTotal,
                      showSizeChanger: true,
                      showQuickJumper: true,
                      showTotal: (total) => `共 ${total} 个用户`,
                    }}
                    onChange={handleUserTableChange}
                  />
                </>
              ),
            },
            {
              key: 'roles',
              label: '角色',
              children: (
                <>
                  <Space style={{ marginBottom: 16 }}>
                    <Button type="primary" onClick={openCreateRole}>创建角色</Button>
                    <Button onClick={refreshRoles}>刷新</Button>
                  </Space>
                  <Table
                    rowKey="id"
                    columns={roleColumns}
                    dataSource={roles}
                    loading={rolesLoading}
                    pagination={false}
                  />

                  <Modal
                    title={editingRole ? '编辑角色' : '创建角色'}
                    open={roleModalOpen}
                    onCancel={() => setRoleModalOpen(false)}
                    onOk={submitRole}
                    okText="保存"
                    cancelText="取消"
                  >
                    <Form form={roleForm} layout="vertical">
                      <Form.Item name="name" label="名称" rules={[{ required: true, message: '请输入角色名称' }]}>
                        <Input />
                      </Form.Item>
                      <Form.Item
                        name="code"
                        label="代码"
                        rules={[{ required: true, message: '请输入角色代码' }]}
                        extra="建议使用小写字母与下划线，例如 admin / problem_setter"
                      >
                        <Input disabled={!!editingRole} />
                      </Form.Item>
                      <Form.Item name="priority" label="优先级" initialValue={10}>
                        <Input type="number" />
                      </Form.Item>
                      <Form.Item name="description" label="描述">
                        <Input.TextArea rows={3} />
                      </Form.Item>
                    </Form>
                  </Modal>
                </>
              ),
            },
            {
              key: 'permissions',
              label: '权限',
              children: (
                <Table
                  rowKey="id"
                  columns={permissionColumns}
                  dataSource={permissions}
                  loading={permissionsLoading}
                  pagination={false}
                />
              ),
            },
          ]}
        />
      </Card>
    </>
  )
}

const AnnouncementManagement = () => (
  <Card title="公告管理">
    <Empty description="暂无系统公告管理功能（后端未提供对应接口）" />
  </Card>
)

const AuditLog = () => {
  const [query, setQuery] = useState<{ page: number; pageSize: number }>({ page: 1, pageSize: 20 })
  const { data, loading, refresh } = useRequest(() => adminService.getAuditLogs(query), { refreshDeps: [query] })

  const columns: ColumnsType<AuditLogDto> = [
    { title: '时间', dataIndex: 'timestamp', key: 'timestamp', render: (v: string) => formatTime(v) },
    { title: '用户', dataIndex: 'username', key: 'username', render: (v?: string | null) => v || '-' },
    { title: '动作', dataIndex: 'action', key: 'action', render: (v: number) => <Tag>{v}</Tag> },
    { title: '描述', dataIndex: 'description', key: 'description' },
    { title: 'IP', dataIndex: 'ipAddress', key: 'ipAddress', render: (v?: string | null) => v || '-' },
  ]

  const onChange = (pagination: TablePaginationConfig) => {
    setQuery({
      page: pagination.current || 1,
      pageSize: pagination.pageSize || 20,
    })
  }

  return (
    <Card
      title="审计日志"
      extra={<Button onClick={refresh}>刷新</Button>}
    >
      <Table
        rowKey="id"
        columns={columns}
        dataSource={data?.data?.items || []}
        loading={loading}
        pagination={{
          current: data?.data?.page || 1,
          pageSize: data?.data?.pageSize || 20,
          total: data?.data?.total || 0,
          showSizeChanger: true,
          showQuickJumper: true,
          showTotal: (total) => `共 ${total} 条日志`,
        }}
        onChange={onChange}
      />
    </Card>
  )
}

const SystemSettings = () => {
  const [messageApi, contextHolder] = message.useMessage()
  const { data: settingsResp, loading: settingsLoading, refresh: refreshSettings } = useRequest(() => adminService.getSettings())
  const { data: judgersResp, loading: judgersLoading, refresh: refreshJudgers } = useRequest(() => adminService.getJudgers())
  const { data: languagesResp, loading: languagesLoading, refresh: refreshLanguages } = useRequest(() => adminService.getLanguages())

  const settings = settingsResp?.data ?? []
  const judgers = judgersResp?.data ?? []
  const languages = languagesResp?.data ?? []

  const [editConfig, setEditConfig] = useState<SystemConfigDto | null>(null)
  const [configForm] = Form.useForm()

  const [judgerConfigModalOpen, setJudgerConfigModalOpen] = useState(false)
  const [judgerConfigGenerating, setJudgerConfigGenerating] = useState(false)
  const [judgerConfigResult, setJudgerConfigResult] = useState<GenerateJudgerConfigResponse | null>(null)
  const [judgerConfigForm] = Form.useForm()

  const [judgerEnvModalOpen, setJudgerEnvModalOpen] = useState(false)
  const [judgerEnvLoading, setJudgerEnvLoading] = useState(false)
  const [judgerEnvResult, setJudgerEnvResult] = useState<JudgerRuntimeStatusResponse | null>(null)

  const downloadTextFile = (filename: string, content: string) => {
    const blob = new Blob([content], { type: 'text/plain;charset=utf-8' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = filename
    document.body.appendChild(a)
    a.click()
    a.remove()
    URL.revokeObjectURL(url)
  }

  const copyToClipboard = async (text: string, successMessage: string) => {
    try {
      if (navigator?.clipboard?.writeText) {
        await navigator.clipboard.writeText(text)
        messageApi.success(successMessage)
        return
      }
    } catch {
      // Fall through to legacy execCommand
    }

    try {
      const el = document.createElement('textarea')
      el.value = text
      el.style.position = 'fixed'
      el.style.opacity = '0'
      document.body.appendChild(el)
      el.focus()
      el.select()
      document.execCommand('copy')
      el.remove()
      messageApi.success(successMessage)
    } catch {
      messageApi.error('复制失败，请手动复制')
    }
  }

  const openEditConfig = (config: SystemConfigDto) => {
    setEditConfig(config)
    configForm.setFieldsValue({ value: config.value })
  }

  const submitConfig = async () => {
    if (!editConfig) return
    const { value } = await configForm.validateFields()
    try {
      await adminService.updateSetting(editConfig.key, String(value))
      messageApi.success('配置已更新')
      setEditConfig(null)
      refreshSettings()
    } catch (e: any) {
      messageApi.error(e?.message || '更新失败')
    }
  }

  const openJudgerConfigModal = () => {
    setJudgerConfigModalOpen(true)
    setJudgerConfigResult(null)
    judgerConfigForm.setFieldsValue({
      name: `judger-${Date.now()}`,
      maxConcurrentTasks: 4,
      mode: 'http',
      pollIntervalMs: 1000,
      workDir: '/tmp/aurora-judge',
      logLevel: 'Information',
    })
  }

  const generateJudgerConfig = async () => {
    const values = await judgerConfigForm.validateFields()
    const payload: GenerateJudgerConfigRequest = {
      name: values.name,
      maxConcurrentTasks: values.maxConcurrentTasks,
      mode: values.mode,
      backendUrl: values.backendUrl,
      pollIntervalMs: values.pollIntervalMs,
      workDir: values.workDir,
      rabbitMqConnection: values.rabbitMqConnection,
      logLevel: values.logLevel,
    }

    setJudgerConfigGenerating(true)
    try {
      const resp = await adminService.generateJudgerConfig(payload)
      setJudgerConfigResult(resp.data || null)
      messageApi.success('Judger 配置已生成')
    } catch (e: any) {
      messageApi.error(e?.message || '生成失败')
    } finally {
      setJudgerConfigGenerating(false)
    }
  }

  const refreshJudgerEnvironment = async () => {
    setJudgerEnvModalOpen(true)
    setJudgerEnvLoading(true)
    setJudgerEnvResult(null)
    try {
      const resp = await adminService.getJudgerRuntimeStatus()
      setJudgerEnvResult(resp.data || null)
      messageApi.success('环境检测已刷新')
    } catch (e: any) {
      messageApi.error(e?.message || '刷新失败')
    } finally {
      setJudgerEnvLoading(false)
    }
  }

  const settingColumns: ColumnsType<SystemConfigDto> = [
    { title: 'Key', dataIndex: 'key', key: 'key' },
    { title: 'Value', dataIndex: 'value', key: 'value', render: (v: string) => <Text code>{v}</Text> },
    { title: '类型', dataIndex: 'type', key: 'type' },
    { title: '分类', dataIndex: 'category', key: 'category', render: (v?: string | null) => v || '-' },
    { title: '公开', dataIndex: 'isPublic', key: 'isPublic', render: (v: boolean) => (v ? <Tag color="green">是</Tag> : <Tag>否</Tag>) },
    { title: '更新时间', dataIndex: 'updatedAt', key: 'updatedAt', render: (v: string) => formatTime(v) },
    {
      title: '操作',
      key: 'actions',
      render: (_, record) => (
        <Button size="small" onClick={() => openEditConfig(record)}>
          编辑
        </Button>
      ),
    },
  ]

  const judgerColumns: ColumnsType<JudgerStatusDto> = [
    { title: '名称', dataIndex: 'name', key: 'name' },
    { title: 'JudgerId', dataIndex: 'judgerId', key: 'judgerId', render: (v: string) => <Text code>{v}</Text> },
    {
      title: '状态',
      dataIndex: 'isOnline',
      key: 'isOnline',
      render: (v: boolean) => <Tag color={v ? 'green' : 'red'}>{v ? '在线' : '离线'}</Tag>,
    },
    {
      title: '启用',
      dataIndex: 'isEnabled',
      key: 'isEnabled',
      render: (v: boolean, record) => (
        <Switch
          checked={v}
          onChange={async (checked) => {
            try {
              await adminService.setJudgerEnabled(record.id, checked)
              messageApi.success(checked ? '已启用' : '已禁用')
              refreshJudgers()
            } catch (e: any) {
              messageApi.error(e?.message || '操作失败')
            }
          }}
        />
      ),
    },
    { title: '任务', key: 'tasks', render: (_, r) => `${r.currentTasks}/${r.maxTasks}` },
    { title: '最后心跳', dataIndex: 'lastHeartbeat', key: 'lastHeartbeat', render: (v?: string | null) => formatTime(v) },
  ]

  const languageColumns: ColumnsType<any> = [
    { title: '代码', dataIndex: 'code', key: 'code' },
    { title: '名称', dataIndex: 'name', key: 'name' },
    { title: '启用', dataIndex: 'isEnabled', key: 'isEnabled', render: (v: boolean) => (v ? <Tag color="green">是</Tag> : <Tag>否</Tag>) },
    { title: '排序', dataIndex: 'order', key: 'order' },
  ]

  return (
    <>
      {contextHolder}
      <Card title="系统设置">
        <Tabs
          items={[
            {
              key: 'settings',
              label: '配置',
              children: (
                <>
                  <Space style={{ marginBottom: 16 }}>
                    <Button onClick={refreshSettings}>刷新</Button>
                  </Space>
                  <Table
                    rowKey="id"
                    columns={settingColumns}
                    dataSource={settings}
                    loading={settingsLoading}
                    pagination={false}
                  />
                </>
              ),
            },
            {
              key: 'judgers',
              label: '判题机',
              children: (
                <>
                  <Space style={{ marginBottom: 16 }}>
                    <Button onClick={refreshJudgers}>状态刷新</Button>
                    <Button icon={<EyeOutlined />} onClick={refreshJudgerEnvironment}>
                      环境检测刷新
                    </Button>
                    <Button type="primary" onClick={openJudgerConfigModal}>
                      配置生成
                    </Button>
                  </Space>
                  <Table
                    rowKey="id"
                    columns={judgerColumns}
                    dataSource={judgers}
                    loading={judgersLoading}
                    pagination={false}
                  />
                </>
              ),
            },
            {
              key: 'languages',
              label: '语言',
              children: (
                <>
                  <Space style={{ marginBottom: 16 }}>
                    <Button onClick={refreshLanguages}>刷新</Button>
                  </Space>
                  <Table
                    rowKey="id"
                    columns={languageColumns}
                    dataSource={languages}
                    loading={languagesLoading}
                    pagination={false}
                  />
                </>
              ),
            },
          ]}
        />

        <Modal
          title={editConfig ? `编辑配置: ${editConfig.key}` : '编辑配置'}
          open={!!editConfig}
          onCancel={() => setEditConfig(null)}
          onOk={submitConfig}
          okText="保存"
          cancelText="取消"
        >
          <Form form={configForm} layout="vertical">
            <Form.Item name="value" label="Value" rules={[{ required: true, message: '请输入配置值' }]}>
              <Input />
            </Form.Item>
          </Form>
        </Modal>

        <Modal
          title="Judger 配置生成"
          open={judgerConfigModalOpen}
          onCancel={() => setJudgerConfigModalOpen(false)}
          onOk={generateJudgerConfig}
          okText="生成"
          cancelText="关闭"
          confirmLoading={judgerConfigGenerating}
          width={720}
        >
          <Form form={judgerConfigForm} layout="vertical">
            <Row gutter={16}>
              <Col span={12}>
                <Form.Item name="name" label="Judger 名称" rules={[{ required: true, message: '请输入 Judger 名称' }]}>
                  <Input placeholder="judger-1" />
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item name="maxConcurrentTasks" label="最大并发任务数" rules={[{ required: true, message: '请输入并发数' }]}>
                  <InputNumber min={1} style={{ width: '100%' }} />
                </Form.Item>
              </Col>
            </Row>

            <Row gutter={16}>
              <Col span={12}>
                <Form.Item name="mode" label="模式" rules={[{ required: true, message: '请选择模式' }]}>
                  <Select
                    options={[
                      { value: 'http', label: 'HTTP' },
                      { value: 'rabbitmq', label: 'RabbitMQ' },
                    ]}
                  />
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item name="logLevel" label="日志级别">
                  <Select
                    options={[
                      { value: 'Trace', label: 'Trace' },
                      { value: 'Debug', label: 'Debug' },
                      { value: 'Information', label: 'Information' },
                      { value: 'Warning', label: 'Warning' },
                      { value: 'Error', label: 'Error' },
                      { value: 'Critical', label: 'Critical' },
                    ]}
                  />
                </Form.Item>
              </Col>
            </Row>

            <Row gutter={16}>
              <Col span={12}>
                <Form.Item name="backendUrl" label="后端地址（HTTP 模式可选）">
                  <Input placeholder="留空自动使用当前站点" />
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item name="pollIntervalMs" label="轮询间隔 (ms)">
                  <InputNumber min={100} style={{ width: '100%' }} />
                </Form.Item>
              </Col>
            </Row>

            <Row gutter={16}>
              <Col span={12}>
                <Form.Item name="workDir" label="工作目录">
                  <Input />
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item name="rabbitMqConnection" label="RabbitMQ 连接串（RabbitMQ 模式可选）">
                  <Input placeholder="amqp://guest:guest@localhost:5672" />
                </Form.Item>
              </Col>
            </Row>
          </Form>

          {judgerConfigResult && (
            <>
              <Divider />
              <Typography.Paragraph type="secondary">
                Secret 只会显示一次；建议立即下载并保存为 <Text code>judger.conf</Text>。
              </Typography.Paragraph>
              <Typography.Paragraph>
                <Text strong>JudgerId：</Text> <Text code>{judgerConfigResult.judgerId}</Text>
              </Typography.Paragraph>
              <Typography.Paragraph>
                <Text strong>Secret：</Text> <Text code copyable>{judgerConfigResult.secret}</Text>
              </Typography.Paragraph>
              <Typography.Paragraph>
                <Text strong>judger.conf：</Text>
              </Typography.Paragraph>
              <Space style={{ marginBottom: 8 }}>
                <Button
                  onClick={() => downloadTextFile('judger.conf', judgerConfigResult.configText)}
                >
                  下载 judger.conf
                </Button>
                <Button
                  onClick={() => copyToClipboard(judgerConfigResult.configText, '配置已复制')}
                >
                  复制配置
                </Button>
              </Space>
              <Input.TextArea readOnly value={judgerConfigResult.configText} autoSize={{ minRows: 8, maxRows: 14 }} />
            </>
          )}
        </Modal>

        <Modal
          title="Judger 环境检测"
          open={judgerEnvModalOpen}
          onCancel={() => setJudgerEnvModalOpen(false)}
          footer={<Button onClick={() => setJudgerEnvModalOpen(false)}>关闭</Button>}
          width={720}
        >
          {judgerEnvLoading ? (
            <Spin />
          ) : (
            <>
              <Typography.Paragraph>
                <Text strong>待处理任务数：</Text> {judgerEnvResult?.pendingTasks ?? '-'}
              </Typography.Paragraph>
              <Typography.Paragraph>
                <Text strong>运行中 Judger：</Text> {judgerEnvResult?.judgers?.length ?? 0}
              </Typography.Paragraph>
              <Input.TextArea readOnly value={JSON.stringify(judgerEnvResult, null, 2)} autoSize={{ minRows: 10, maxRows: 16 }} />
            </>
          )}
        </Modal>
      </Card>
    </>
  )
}

const AdminPage = () => {
  const navigate = useNavigate()
  const location = useLocation()
  
  const menuItems: MenuProps['items'] = [
    {
      key: '/admin',
      icon: <DashboardOutlined />,
      label: '仪表盘',
    },
    {
      key: '/admin/problems',
      icon: <CodeOutlined />,
      label: '题目管理',
    },
    {
      key: '/admin/submissions',
      icon: <FileTextOutlined />,
      label: '提交管理',
    },
    {
      key: '/admin/contests',
      icon: <TrophyOutlined />,
      label: '比赛管理',
    },
    {
      key: '/admin/users',
      icon: <UserOutlined />,
      label: '用户管理',
    },
    {
      key: '/admin/announcements',
      icon: <NotificationOutlined />,
      label: '公告管理',
    },
    {
      key: '/admin/audit',
      icon: <AuditOutlined />,
      label: '审计日志',
    },
    {
      key: '/admin/settings',
      icon: <SettingOutlined />,
      label: '系统设置',
    },
  ]
  
  const handleMenuClick: MenuProps['onClick'] = (e) => {
    navigate(e.key)
  }
  
  return (
    <Layout>
      <Sider width={200} theme="light">
        <Menu
          mode="inline"
          selectedKeys={[location.pathname]}
          items={menuItems}
          onClick={handleMenuClick}
          style={{ height: '100%' }}
        />
      </Sider>
      <Content style={{ padding: '0 24px' }}>
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/problems" element={<ProblemManagement />} />
          <Route path="/submissions" element={<SubmissionManagement />} />
          <Route path="/contests" element={<ContestManagement />} />
          <Route path="/users" element={<UserManagement />} />
          <Route path="/announcements" element={<AnnouncementManagement />} />
          <Route path="/audit" element={<AuditLog />} />
          <Route path="/settings" element={<SystemSettings />} />
        </Routes>
      </Content>
    </Layout>
  )
}

export default AdminPage
