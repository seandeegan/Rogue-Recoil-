using Sandbox;

namespace RogueRecoil.Modifiers;
public interface IModifier
{
	void Apply( ArenaPlayer player );
	void Remove( ArenaPlayer player );
}
