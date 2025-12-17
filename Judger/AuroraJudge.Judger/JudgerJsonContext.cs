using System.Text.Json.Serialization;
using AuroraJudge.Judger.Contracts;

namespace AuroraJudge.Judger;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(JudgerConnectRequest))]
[JsonSerializable(typeof(JudgerConnectResponse))]
[JsonSerializable(typeof(JudgerHeartbeatRequest))]
[JsonSerializable(typeof(JudgerFetchRequest))]
[JsonSerializable(typeof(JudgerFetchResponse))]
[JsonSerializable(typeof(JudgeTaskResponse))]
[JsonSerializable(typeof(JudgeTestCaseResponse))]
[JsonSerializable(typeof(JudgerReportRequest))]
[JsonSerializable(typeof(TestCaseResultRequest))]
[JsonSerializable(typeof(JudgeTask))]
[JsonSerializable(typeof(JudgeResponse))]
[JsonSerializable(typeof(JudgerHeartbeat))]
[JsonSerializable(typeof(TestCaseData))]
[JsonSerializable(typeof(TestCaseResult))]
internal partial class JudgerJsonContext : JsonSerializerContext
{
}
