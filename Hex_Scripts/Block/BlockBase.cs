using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class BlockBase : WorldObject, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    //--------------------------------------------------
    #region Delegate
    public Action<BlockBase> OnPressed { get; set; }
    public Func<BlockBase, float, BlockBase> OnDragged { get; set; }
    public Func<BlockBase, BlockBase, bool> OnFoundTargetBlock { get; set; }
    #endregion

    #region Properties
    public BlockInstance CurrentBlock { get; set; } = new();
    public bool IsFalling { get; private set; }
    public float DropDelay { get; set; }
    public bool IsLocked { get; set; }
    public bool IsNewSpawn { get; set; }
    public bool HasPendingPath => _dropPathQueue.Count > 0 || IsFalling;
    #endregion

    #region Fields
    private readonly Queue<Vector3> _dropPathQueue = new();
    private Vector3 _curTargetPos;

    private bool _isSwapping;
    private bool _isPressing;
    private Vector2 _pressedPosition;
    #endregion

    //--------------------------------------------------
    #region Interface Methods
    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsLocked || IsFalling) return;

        _pressedPosition = eventData.position;

        _isPressing = true;
        OnPressed?.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsLocked || IsFalling) return;
        if (!_isPressing || _isSwapping) return;

        var rowDir = eventData.position - _pressedPosition;
        var dist = rowDir.magnitude;
        if (dist < 30.0f || dist >= 300.0f) return;

        var dir = rowDir.normalized;
        float angle = Vector2.SignedAngle(Vector2.up, dir);

        var targetBlock = OnDragged?.Invoke(this, angle);
        if (targetBlock != null)
        {
            _isSwapping = true;
            OnFoundTargetBlock?.Invoke(this, targetBlock);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        CancelDrag();
    }
    #endregion

    //--------------------------------------------------
    #region Methods
    private void Awake()
    {
        SetUpOnAwake();
        LoadComponenets();
    }

    public void UpdateBlockSkin(ref BlockSkin blockSkin)
    {
        MainRenderer.sprite = blockSkin.sprites[0];
        CurrentBlock.Skin = blockSkin;
    }

    public void UpdateBlockSprite(int spriteIdx)
    {
        if(CurrentBlock.Skin.sprites.Length <= spriteIdx) 
            return;

        MainRenderer.sprite = CurrentBlock.Skin.sprites[spriteIdx];
    }

    public override void ResetStatus()
    {
        base.ResetStatus();

        IsFalling = false;
        DropDelay = 0.0f;
        _dropPathQueue.Clear();
        _curTargetPos = Vector3.zero;

        _isSwapping = false;
        _isPressing = false;
        _pressedPosition = Vector2.zero;

        CurrentBlock.Cell = default;
        CurrentBlock.Life = 0;
        CurrentBlock.Skin = default;

        OnPressed = null;
        OnDragged = null;
        OnFoundTargetBlock = null;

        MainRenderer.sprite = null;
    }

    public virtual void ReleaseBlock() => BlockManager.Instance.ReleaseBlock(this);
    #endregion

    #region Update Methods
    private void Update()
    {
        if(DropDelay > 0.0f)
        {
            DropDelay -= Time.deltaTime;

            if (DropDelay > 0.0f) return;
            DropDelay = 0.0f;
        }

        if (!IsFalling)
            return;

        var dropFallingSpeed = BlockManager.Instance.BlockInfoSO.DropFallingSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, _curTargetPos, dropFallingSpeed);

        if((transform.position - _curTargetPos).sqrMagnitude <= 0.01f)
        {
            transform.position = _curTargetPos;

            if(_dropPathQueue.Count > 0)
            {
                _curTargetPos = _dropPathQueue.Dequeue();
            }

            else
            {
                IsFalling = false;
                DropDelay = 0.0f;
            }
        }
    }

    public void SetUpDropPath(IEnumerable<Vector3> dropPath, float beginDropDelay)
    {
        _dropPathQueue.Clear();

        if(dropPath != null)
        {
            foreach (var target in dropPath)
                _dropPathQueue.Enqueue(target);
        }

        if (_dropPathQueue.Count > 0)
        {
            _curTargetPos = _dropPathQueue.Dequeue();
            DropDelay = Mathf.Max(0.0f, beginDropDelay);
            IsFalling = true;
        }

        else
        {
            DropDelay = 0.0f;
            IsFalling = false;
        }
    }

    public void AddDropPath(IEnumerable<Vector3> addPath, float beginDropDelay)
    {
        foreach(var path in addPath)
            _dropPathQueue.Enqueue(path);

        if(!IsFalling && _dropPathQueue.Count > 0)
        {
            _curTargetPos = _dropPathQueue.Dequeue();
            DropDelay = Mathf.Max(0.0f, beginDropDelay);
            IsFalling = true;
        }
    }
    #endregion

    #region Drag Methods
    public void CancelDrag()
    {
        _isSwapping = false;
        _isPressing = false;
    }
    #endregion
}
