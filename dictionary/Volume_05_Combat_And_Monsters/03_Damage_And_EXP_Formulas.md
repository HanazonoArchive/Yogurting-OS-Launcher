# Volume 5, Chapter 3: Combat Damage, Critical Scaling & EXP Formulas

## 1. Primary Attack Damage Formulas

Decompiled from Delphi server combat engine (`server_legacy/DELPHI PROJECT/_Unit49.pas`):

### 1. Base Attack Power Calculation
$$\text{BaseAttack} = (\text{POW} \times 4) + \sum \text{EquippedWeaponAttack}$$
* **`POW`**: Character Power stat loaded from `StatusTable.txt` for the current base level + accessory bonuses.
* **`EquippedWeaponAttack`**: Attack stat from `ItemTable.txt` for the item in Paperdoll Slot 4.

### 2. Final Hit Damage & Variance
$$\text{Damage} = \max\Big(5, \big(\text{BaseAttack} + \text{Random}(-3, +8)\big) \times \text{DamageMultiplier}\Big)$$
* **`DamageMultiplier`**: Standard value `1.0f`, increased by active attack buffs (e.g. `1.2f` with Tempo attack buff).

---

## 2. Critical Hit Scaling (LUCK Stat)

$$\text{CritChance (\%)} = \min\Big(60.0\%, \big(\text{LUCK} \times 1.5 + 5.0\big) \times \text{CritBuffMultiplier}\Big)$$

* If a randomly rolled value (`0..99`) is less than $\text{CritChance}$:
  $$\text{Final Damage} = \lfloor \text{Damage} \times 1.5 \rfloor$$
  * The `isCritical` flag in `0x791A (MsgGameAttackAns)` is set to `1`, triggering yellow bold numbers in the client.

---

## 3. Combo Streak Timer & Threshold

* **Combo Accumulation**: Every successful hit increments `player.ComboCount` by `+1` (maximum `999`).
* **Combo Reset Threshold**: `5000 ms` (5 seconds).
* **Rule**: If $\text{CurrentTimestamp} - \text{LastAttackTime} > 5000\text{ms}$, the combo counter resets to `0`.

---

## 4. Monster Experience (EXP) Level-Difference Curve

Decompiled directly from `_Unit49.pas:18999`:

$$\text{LevelDiff} = |\text{PlayerLevel} - \text{MonsterLevel}|$$

$$\text{ScalingFactor} = 1.0 - \big(0.003907 \times \text{LevelDiff}^2\big)$$

$$\text{ExpEarned} = \begin{cases} 
1 & \text{if } \text{LevelDiff} > 15 \\ 
\max\Big(1, \lfloor \text{MonsterBaseExp} \times \max(0.1, \text{ScalingFactor}) \rfloor\Big) \times \text{ExpBuffMultiplier} & \text{if } \text{LevelDiff} \le 15 
\end{cases}$$

### Example EXP Rewards:
| Monster Level | Player Level | Level Difference | Scaling Factor | Monster Base EXP | Final EXP Earned |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Lvl 5** | **Lvl 5** | `0` | `1.000` (100%) | `50 EXP` | **`50 EXP`** |
| **Lvl 5** | **Lvl 7** | `2` | `0.984` (98.4%) | `50 EXP` | **`49 EXP`** |
| **Lvl 5** | **Lvl 10** | `5` | `0.902` (90.2%) | `50 EXP` | **`45 EXP`** |
| **Lvl 5** | **Lvl 15** | `10` | `0.609` (60.9%) | `50 EXP` | **`30 EXP`** |
| **Lvl 5** | **Lvl 21** | `16` | `LevelDiff > 15` | `50 EXP` | **`1 EXP`** |

---

## 5. Monster Counter-Attack Damage Formula

$$\text{MonsterDamage} = \max\left(1, (\text{MonsterLevel} \times 2) - \left\lfloor \frac{\text{PlayerDefense}}{4} \right\rfloor\right)$$

* When an aggroed monster executes an attack cycle against the player, damage is subtracted from `Player.CurrentHp` and synchronized via `0x520F (MsgGameSetStateNtf)`.
