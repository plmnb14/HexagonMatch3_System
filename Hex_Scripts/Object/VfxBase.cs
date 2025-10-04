using UnityEngine;

public class VfxBase : WorldObject
{
    //--------------------------------------------------
    #region Fields
    [SerializeField] private MeshRenderer[] _childRenderers;

    private MaterialPropertyBlock _mpb;
    #endregion

    //--------------------------------------------------
    #region Methopds
    private void Awake()
    {
        SetUpOnAwake();
        LoadComponenets();
        SetUpMaterial();
    }

    public void SetUpMaterial()
    {
        if(_mpb == null)
            _mpb = new MaterialPropertyBlock();
    }

    public virtual void UpdateParticleSprite(ref BlockSkin blockSkin)
    {
        _mpb.SetTexture("_MainTex", blockSkin.popParticleSprite.texture);

        for (int i = 0; i < _childRenderers.Length; i++)
            _childRenderers[i].SetPropertyBlock(_mpb);
    }

    public virtual void ReleaseVfx() => BlockManager.Instance.ReleaseBlockPopVfx(this);
    #endregion
}
