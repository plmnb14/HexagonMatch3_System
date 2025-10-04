using System;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockInfo", menuName = "Scriptable Objects/BlockInfo")]
public class BlockInfo : ScriptableObject
{
    //--------------------------------------------------
    #region Fields
    [SerializeField] private float _dropFallingSpeed = 2.0f;
    public float DropFallingSpeed => _dropFallingSpeed;
    
    [SerializeField] private BlockBase _normalBlockPrefab;
    public BlockBase NormalBlockPrefab => _normalBlockPrefab;

    [SerializeField] private BlockSkin[] _skins;
    public BlockSkin[] Skins => _skins;

    [SerializeField] private VfxBase _blockPopVfxPrefab;
    public VfxBase PopVfxPrefab => _blockPopVfxPrefab;
    #endregion
}

[Serializable] public struct BlockSkin
{
    public BlockCategory category;
    public BlockDetailType detailType;
    public Sprite[] sprites;
    public Sprite popParticleSprite;
}

[Serializable] public struct PlacedBlockInstance
{
    public Vector2Int cell;
    public BlockCategory category;
    public BlockDetailType detailType;
}