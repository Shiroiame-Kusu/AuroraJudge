namespace AuroraJudge.Shared.Constants;

/// <summary>
/// 权限常量定义
/// </summary>
public static class Permissions
{
    // 超级权限
    public const string All = "*";
    
    // 用户管理
    public const string UserView = "user:view";
    public const string UserCreate = "user:create";
    public const string UserEdit = "user:edit";
    public const string UserDelete = "user:delete";
    public const string UserBan = "user:ban";
    public const string UserViewProfile = "user:view:profile";
    public const string UserEditProfile = "user:edit:profile";
    
    // 题目管理
    public const string ProblemView = "problem.view";
    public const string ProblemViewHidden = "problem.view.hidden";
    public const string ProblemCreate = "problem.create";
    public const string ProblemEdit = "problem.edit";
    public const string ProblemDelete = "problem.delete";
    public const string ProblemRejudge = "problem.rejudge";
    public const string ProblemManageTestCases = "problem.testcases";
    public const string ProblemViewTestData = "problem.view.testdata";
    
    // 提交管理
    public const string SubmissionView = "submission.view";
    public const string SubmissionViewAll = "submission.view.all";
    public const string SubmissionViewCode = "submission.view.code";
    public const string SubmissionCreate = "submission.create";
    public const string SubmissionRejudge = "submission.rejudge";
    
    // 比赛管理
    public const string ContestView = "contest.view";
    public const string ContestViewHidden = "contest.view.hidden";
    public const string ContestCreate = "contest.create";
    public const string ContestEdit = "contest.edit";
    public const string ContestDelete = "contest.delete";
    public const string ContestManageParticipants = "contest.participants";
    public const string ContestViewAllSubmissions = "contest.submissions.all";
    public const string ContestAnnouncement = "contest.announcement";
    
    // 标签管理
    public const string TagView = "tag.view";
    public const string TagCreate = "tag.create";
    public const string TagEdit = "tag.edit";
    public const string TagDelete = "tag.delete";
    
    // 公告管理
    public const string AnnouncementView = "announcement.view";
    public const string AnnouncementCreate = "announcement.create";
    public const string AnnouncementEdit = "announcement.edit";
    public const string AnnouncementDelete = "announcement.delete";
    
    // 系统管理
    public const string SystemSettings = "system.settings";
    public const string SystemAuditLog = "system.audit";
    public const string SystemJudger = "system.judger";
    public const string SystemLanguages = "system.languages";
    public const string SystemFeatureFlags = "system.features";
    
    // 角色权限管理
    public const string RoleView = "role.view";
    public const string RoleCreate = "role.create";
    public const string RoleEdit = "role.edit";
    public const string RoleDelete = "role.delete";
    public const string PermissionView = "permission.view";
    public const string PermissionAssign = "permission.assign";
    
    /// <summary>
    /// 获取所有权限列表
    /// </summary>
    public static IReadOnlyList<PermissionDefinition> GetAll()
    {
        return new List<PermissionDefinition>
        {
            // 用户管理
            new(UserView, "查看用户", "用户管理", 1),
            new(UserCreate, "创建用户", "用户管理", 2),
            new(UserEdit, "编辑用户", "用户管理", 3),
            new(UserDelete, "删除用户", "用户管理", 4),
            new(UserBan, "禁用用户", "用户管理", 5),
            new(UserViewProfile, "查看个人资料", "用户管理", 6),
            new(UserEditProfile, "编辑个人资料", "用户管理", 7),
            
            // 题目管理
            new(ProblemView, "查看题目", "题目管理", 10),
            new(ProblemViewHidden, "查看隐藏题目", "题目管理", 11),
            new(ProblemCreate, "创建题目", "题目管理", 12),
            new(ProblemEdit, "编辑题目", "题目管理", 13),
            new(ProblemDelete, "删除题目", "题目管理", 14),
            new(ProblemRejudge, "重判题目", "题目管理", 15),
            new(ProblemManageTestCases, "管理测试数据", "题目管理", 16),
            new(ProblemViewTestData, "查看测试数据", "题目管理", 17),
            
            // 提交管理
            new(SubmissionView, "查看提交", "提交管理", 20),
            new(SubmissionViewAll, "查看所有提交", "提交管理", 21),
            new(SubmissionViewCode, "查看提交代码", "提交管理", 22),
            new(SubmissionCreate, "创建提交", "提交管理", 23),
            new(SubmissionRejudge, "重判提交", "提交管理", 24),
            
            // 比赛管理
            new(ContestView, "查看比赛", "比赛管理", 30),
            new(ContestViewHidden, "查看隐藏比赛", "比赛管理", 31),
            new(ContestCreate, "创建比赛", "比赛管理", 32),
            new(ContestEdit, "编辑比赛", "比赛管理", 33),
            new(ContestDelete, "删除比赛", "比赛管理", 34),
            new(ContestManageParticipants, "管理参赛者", "比赛管理", 35),
            new(ContestViewAllSubmissions, "查看所有比赛提交", "比赛管理", 36),
            new(ContestAnnouncement, "发布比赛公告", "比赛管理", 37),
            
            // 标签管理
            new(TagView, "查看标签", "标签管理", 40),
            new(TagCreate, "创建标签", "标签管理", 41),
            new(TagEdit, "编辑标签", "标签管理", 42),
            new(TagDelete, "删除标签", "标签管理", 43),
            
            // 公告管理
            new(AnnouncementView, "查看公告", "公告管理", 50),
            new(AnnouncementCreate, "创建公告", "公告管理", 51),
            new(AnnouncementEdit, "编辑公告", "公告管理", 52),
            new(AnnouncementDelete, "删除公告", "公告管理", 53),
            
            // 系统管理
            new(SystemSettings, "系统设置", "系统管理", 60),
            new(SystemAuditLog, "审计日志", "系统管理", 61),
            new(SystemJudger, "判题机管理", "系统管理", 62),
            new(SystemLanguages, "语言配置", "系统管理", 63),
            new(SystemFeatureFlags, "功能开关", "系统管理", 64),
            
            // 角色权限管理
            new(RoleView, "查看角色", "权限管理", 70),
            new(RoleCreate, "创建角色", "权限管理", 71),
            new(RoleEdit, "编辑角色", "权限管理", 72),
            new(RoleDelete, "删除角色", "权限管理", 73),
            new(PermissionView, "查看权限", "权限管理", 74),
            new(PermissionAssign, "分配权限", "权限管理", 75),
        };
    }
}

/// <summary>
/// 权限定义
/// </summary>
public record PermissionDefinition(string Code, string Name, string Category, int Order);

/// <summary>
/// 角色常量定义
/// </summary>
public static class Roles
{
    public const string SuperAdmin = "super_admin";
    public const string Admin = "admin";
    public const string ProblemSetter = "problem_setter";
    public const string Moderator = "moderator";
    public const string User = "user";
    public const string Guest = "guest";
    
    // 预定义角色 ID
    public static readonly Guid AdminRoleId = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid ProblemSetterRoleId = new("00000000-0000-0000-0000-000000000002");
    public static readonly Guid UserRoleId = new("00000000-0000-0000-0000-000000000003");
    
    /// <summary>
    /// 获取默认角色定义
    /// </summary>
    public static IReadOnlyList<RoleDefinition> GetDefaults()
    {
        return new List<RoleDefinition>
        {
            new(SuperAdmin, "超级管理员", "拥有系统所有权限", true, 100, Permissions.GetAll().Select(p => p.Code).ToArray()),
            new(Admin, "管理员", "拥有大部分管理权限", true, 90, GetAdminPermissions()),
            new(ProblemSetter, "出题人", "可以创建和管理题目", true, 50, GetProblemSetterPermissions()),
            new(Moderator, "版主", "可以管理用户和内容", true, 40, GetModeratorPermissions()),
            new(User, "普通用户", "基础用户权限", true, 10, GetUserPermissions()),
            new(Guest, "访客", "仅查看公开内容", true, 0, GetGuestPermissions()),
        };
    }
    
    private static string[] GetAdminPermissions() => new[]
    {
        Permissions.UserView, Permissions.UserCreate, Permissions.UserEdit, Permissions.UserBan,
        Permissions.ProblemView, Permissions.ProblemViewHidden, Permissions.ProblemCreate,
        Permissions.ProblemEdit, Permissions.ProblemDelete, Permissions.ProblemRejudge,
        Permissions.ProblemManageTestCases, Permissions.ProblemViewTestData,
        Permissions.SubmissionView, Permissions.SubmissionViewAll, Permissions.SubmissionViewCode,
        Permissions.SubmissionRejudge,
        Permissions.ContestView, Permissions.ContestViewHidden, Permissions.ContestCreate,
        Permissions.ContestEdit, Permissions.ContestDelete, Permissions.ContestManageParticipants,
        Permissions.ContestViewAllSubmissions, Permissions.ContestAnnouncement,
        Permissions.TagView, Permissions.TagCreate, Permissions.TagEdit, Permissions.TagDelete,
        Permissions.AnnouncementView, Permissions.AnnouncementCreate, Permissions.AnnouncementEdit,
        Permissions.AnnouncementDelete,
        Permissions.SystemJudger, Permissions.SystemLanguages, Permissions.SystemAuditLog,
        Permissions.RoleView, Permissions.PermissionView,
    };
    
    private static string[] GetProblemSetterPermissions() => new[]
    {
        Permissions.ProblemView, Permissions.ProblemCreate, Permissions.ProblemEdit,
        Permissions.ProblemManageTestCases, Permissions.ProblemViewTestData,
        Permissions.ContestView, Permissions.ContestCreate, Permissions.ContestEdit,
        Permissions.ContestAnnouncement,
        Permissions.TagView, Permissions.TagCreate,
        Permissions.SubmissionView, Permissions.SubmissionViewCode,
        Permissions.UserViewProfile, Permissions.UserEditProfile,
    };
    
    private static string[] GetModeratorPermissions() => new[]
    {
        Permissions.UserView, Permissions.UserBan,
        Permissions.ProblemView,
        Permissions.SubmissionView, Permissions.SubmissionViewAll,
        Permissions.ContestView,
        Permissions.TagView,
        Permissions.AnnouncementView, Permissions.AnnouncementCreate, Permissions.AnnouncementEdit,
        Permissions.UserViewProfile, Permissions.UserEditProfile,
    };
    
    private static string[] GetUserPermissions() => new[]
    {
        Permissions.ProblemView,
        Permissions.SubmissionView, Permissions.SubmissionCreate,
        Permissions.ContestView,
        Permissions.TagView,
        Permissions.AnnouncementView,
        Permissions.UserViewProfile, Permissions.UserEditProfile,
    };
    
    private static string[] GetGuestPermissions() => new[]
    {
        Permissions.ProblemView,
        Permissions.ContestView,
        Permissions.AnnouncementView,
    };
}

/// <summary>
/// 角色定义
/// </summary>
public record RoleDefinition(string Code, string Name, string Description, bool IsSystem, int Priority, string[] Permissions);
