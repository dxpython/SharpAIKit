using SharpAIKit.Common;

namespace SharpAIKit.Skill;

/// <summary>
/// Skill 解析器：负责发现、激活和合并 Skill 约束
/// </summary>
public interface ISkillResolver
{
    /// <summary>注册一个 Skill</summary>
    /// <param name="skill">要注册的 Skill</param>
    void RegisterSkill(ISkill skill);
    
    /// <summary>
    /// 根据任务和上下文解析应该激活的 Skill，并返回完整的解析结果
    /// </summary>
    /// <param name="task">用户任务</param>
    /// <param name="context">当前上下文</param>
    /// <returns>Skill 解析结果（包含激活的 Skill、合并后的约束和决策原因）</returns>
    SkillResolutionResult Resolve(string task, StrongContext context);
    
    /// <summary>获取所有已注册的 Skill（仅元数据）</summary>
    /// <returns>Skill 元数据列表</returns>
    IReadOnlyList<SkillMetadata> GetAllSkills();
}

