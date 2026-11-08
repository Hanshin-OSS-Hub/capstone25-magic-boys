using UnityEngine;

// 연출용 프리팹/사운드 모음. Resources/FxLibrary.asset 으로 저장되어 코드 어디서든 불러 쓴다.
// (Tools/Magic Boys/Setup Fx Library 로 자동 생성)
[CreateAssetMenu(menuName = "Magic Boys/Fx Library")]
public class FxLibrary : ScriptableObject
{
    [Header("Meteor / Lava")]
    public GameObject meteorStrike;     // SineVFX MeteorStrike (운석 + 폭발)
    public GameObject lavaPillar;       // SineVFX MoltenPillar
    public GameObject lavaPool;         // SineVFX ConjureLavaPool

    [Header("Sounds")]
    public AudioClip meteorImpact;
    public AudioClip bossSmashImpact;
    public AudioClip bossRush;
    public AudioClip lavaBurst;

    private static FxLibrary instance;
    private static bool loaded;

    public static FxLibrary Instance
    {
        get
        {
            if (!loaded)
            {
                instance = Resources.Load<FxLibrary>("FxLibrary");
                loaded = true;
            }
            return instance;
        }
    }
}
