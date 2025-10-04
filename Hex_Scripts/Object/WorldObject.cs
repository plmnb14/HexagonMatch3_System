using UnityEngine;

public abstract class WorldObject : MonoBehaviour
{
    //--------------------------------------------------
    #region Fields
    public SpriteRenderer MainRenderer => _spriteRenderer;
    protected SpriteRenderer _spriteRenderer;
    #endregion

    //--------------------------------------------------
    #region Methods
    protected virtual void SetUpOnAwake() { }

    protected virtual void LoadComponenets()
    {
        if (_spriteRenderer == null)
            TryGetComponent(out _spriteRenderer);
    }

    public virtual void ResetStatus() { transform.position = Vector3.zero; }
    #endregion

}
