# Task 2

### 1. Create `checkBoard`

- Initialize a new board named `checkBoard`, using `ScriptableObject`, with the same structure as the main board.
- Set the board size:
`x = 5`
`y = 1`

- Move all child elements of `checkBoard` down to:
`position.y = -4f`

### 2. Remove automatic board processing

Remove the following behaviors immediately after the board is created:

- Drag functionality.
- Find Matches.
- Collapse.

### 3. Change the initial board filling logic

Modify the board initialization logic:

- Divide the board into groups of 3 cells.
- For each group:
  1. Randomize one item type.
  2. Duplicate that type to all 3 cells in the group.
- This produces groups in the following form:

```text
AAA BBB CCC DDD ...
```

- After all groups have been generated, shuffle the cells to create the final board layout.
- The generated board must guarantee that all items in the board can eventually be cleared.

### 4. Replace Drag with Click

Replace the drag interaction with click interaction:
- When the player clicks a cell on the main board:
  1. Check `checkBoard` for an empty cell.
  2. If an empty cell exists, move the clicked item's item to the first available empty cell in `checkBoard`.
  3. The item is removed from its original position on the main board.

### 5. Win / Game Over conditions

- Win: The main board becomes empty (`board == null`).
- Game Over: `checkBoard` becomes full.

### 6. Auto Win / Auto Lose

- Select the first cell containing an item as the --focus cell--.
- Iterate through the main board and create a list of cells containing the same item type as the focus cell.
- The number of selected cells depends on the desired result:
  1. Auto Win: select 3 matching cells.
  2. Auto Lose: select 5 matching cells.
- Move all selected items from the main board to the available cells in `checkBoard`.
- This allows the system to automatically trigger the corresponding Win or Game Over state.



