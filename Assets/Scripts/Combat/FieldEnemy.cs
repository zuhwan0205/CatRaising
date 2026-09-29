using UnityEngine;

public class FieldEnemy : MonoBehaviour
{
    public CombatHealth Health { get; private set; }
    public bool Alive => gameObject.activeSelf && !Health.Dead;
    public float RespawnRemaining;
    private SpriteRenderer artwork;
    private Transform healthBar;
    private Color color;
    private float flashRemaining;
    private float slowRemaining;
    public float MovementMultiplier => slowRemaining>0?.6f:1f;

    public void ApplyIceSlow()
    {
        if(Alive)slowRemaining=2f;
    }

    public void Initialize(SpriteRenderer art, Transform bar, double hp)
    {
        artwork=art; color=art.color; healthBar=bar; Health=new CombatHealth(hp);
    }
    public void StepVisual(float delta)
    {
        flashRemaining=Mathf.Max(0,flashRemaining-delta);
        slowRemaining=Mathf.Max(0,slowRemaining-delta);
        artwork.color=flashRemaining>0?Color.white:slowRemaining>0?Color.Lerp(color,new Color(.35f,.8f,1),.65f):color;
    }
    public bool Hit(double damage)
    {
        if(!Alive)return false;
        bool killed=Health.Damage(damage);
        healthBar.localScale=new Vector3((float)(Health.Current/Health.Maximum),1,1);
        flashRemaining=.15f;
        artwork.color=Color.white;
        if(killed)gameObject.SetActive(false);
        return killed;
    }
    public void Respawn(Vector3 position)
    {
        transform.position=position; Health.Reset(); flashRemaining=0;slowRemaining=0;
        artwork.color=color;healthBar.localScale=Vector3.one;gameObject.SetActive(true);
    }
}
