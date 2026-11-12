namespace Game1;

public class Skills
{
	public static MyHashTable skills = new MyHashTable();

	public static void add(Skill skill)
	{
		skills.put(skill.skillId, skill);
	}

	public static Skill get(short skillId)
	{
		Skill skill = (Skill)skills.get(skillId);
		if (skill == null)
		{
			skill = new Skill();
			skill.skillId = skillId;
			skill.template = new SkillTemplate();
			skill.template.id = 0;
			skill.template.name = "Unknown Skill";
			skill.template.skills = new Skill[1] { skill };
			skills.put(skillId, skill);
		}
		return skill;
	}
}
