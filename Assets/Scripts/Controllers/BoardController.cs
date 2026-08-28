using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    public event Action OnMoveEvent = delegate { };

    public bool IsBusy { get; private set; }

    private Board m_board;
    private Board m_checkBoard;

    private GameManager m_gameManager;

    private bool m_isDragging;

    private Camera m_cam;

    private Collider2D m_hitCollider;

    private GameSettings m_gameSettings;
    private GameSettings m_checkBoardSettings;

    private List<Cell> m_potentialMatch;

    private float m_timeAfterFill;

    private bool m_hintIsShown;

    private bool m_gameOver;

    private Transform checkBoardTransform;

    public void StartGame(GameManager gameManager, GameSettings gameSettings, GameSettings checkBoardSettings)
    {
        m_gameManager = gameManager;

        m_gameSettings = gameSettings;
        m_checkBoardSettings = checkBoardSettings;

        m_gameManager.StateChangedAction += OnGameStateChange;

        m_cam = Camera.main;

        m_board = new Board(this.transform, gameSettings);

        checkBoardTransform = new GameObject("CheckBoard").transform;
        m_checkBoard = new Board(checkBoardTransform, m_checkBoardSettings);
        for (int i = 0; i < checkBoardTransform.childCount; i++)
        {
            Transform child = checkBoardTransform.GetChild(i);
            child.localPosition += new Vector3(0f, -4f, 0f);
        }

        Fill();
    }

    private void Fill()
    {
        m_board.Fill();
        FindMatchesAndCollapse();
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED:
                IsBusy = false;
                break;
            case GameManager.eStateGame.PAUSE:
                IsBusy = true;
                break;
            case GameManager.eStateGame.GAME_OVER:
                m_gameOver = true;
                break;
        }
    }

    public void Update()
    {
        if (m_gameOver) return;
        if (IsBusy) return;

        if (m_board.IsEmpty())
        {
            IsBusy = false;
            m_gameManager.SetState(GameManager.eStateGame.WIN);
            return;
        }

        Cell c2 = GetEmptyCheckBoardCell();
        if (c2 == null)
            m_gameManager.SetState(GameManager.eStateGame.GAME_OVER);

        if (Input.GetMouseButtonDown(0))
        {
            var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                Cell c1 = hit.collider.GetComponent<Cell>();

                if (c1 != null && c1.Item != null && c1.transform.IsChildOf(this.transform))
                { 
                    if (c2 != null)
                    {
                        IsBusy = true;

                        SetSortingLayer(c1, c2);

                        Item item = c1.Item;

                        c1.Free();
                        c2.Assign(item);

                        item.View.DOMove(c2.transform.position, 0.3f).OnComplete(() => { StartCoroutine(CheckCheckBoardMatchesCoroutine()); });
                    }
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            ResetRayCast();
        }
    }

    private Cell GetEmptyCheckBoardCell()
    {
        for (int i = 0; i < checkBoardTransform.childCount; i++)
        {
            Cell cell = checkBoardTransform.GetChild(i).GetComponent<Cell>();

            if (cell != null && cell.IsEmpty)
            {
                return cell;
            }
        }

        return null;
    }
    private IEnumerator CheckCheckBoardMatchesCoroutine()
    {
        while (true)
        {
            bool foundMatch = false;

            for (int i = 0; i <= checkBoardTransform.childCount - 3; i++)
            {
                Cell cell1 = checkBoardTransform.GetChild(i).GetComponent<Cell>();
                Cell cell2 = checkBoardTransform.GetChild(i + 1).GetComponent<Cell>();
                Cell cell3 = checkBoardTransform.GetChild(i + 2).GetComponent<Cell>();

                if (cell1 == null || cell2 == null || cell3 == null)
                    continue;

                if (cell1.Item == null || cell2.Item == null || cell3.Item == null)
                    continue;

                if (cell1.Item.IsSameType(cell2.Item) && cell1.Item.IsSameType(cell3.Item))
                {
                    cell1.ExplodeItem();
                    cell2.ExplodeItem();
                    cell3.ExplodeItem();

                    foundMatch = true;

                    yield return new WaitForSeconds(0.2f);

                    break;
                }
            }

            if (!foundMatch)
                break;
        }

        IsBusy = false;
    }

    private void ResetRayCast()
    {
        m_isDragging = false;
        m_hitCollider = null;
    }

    private void FindMatchesAndCollapse(Cell cell1, Cell cell2)
    {
        if (cell1.Item is BonusItem)
        {
            cell1.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else if (cell2.Item is BonusItem)
        {
            cell2.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else
        {
            List<Cell> cells1 = GetMatches(cell1);
            List<Cell> cells2 = GetMatches(cell2);

            List<Cell> matches = new List<Cell>();
            matches.AddRange(cells1);
            matches.AddRange(cells2);
            matches = matches.Distinct().ToList();

            if (matches.Count < m_gameSettings.MatchesMin)
            {
                m_board.Swap(cell1, cell2, () =>
                {
                    IsBusy = false;
                });
            }
            else
            {
                OnMoveEvent();

                CollapseMatches(matches, cell2);
            }
        }
    }

    private void FindMatchesAndCollapse()
    {
        List<Cell> matches = m_board.FindFirstMatch();

        if (matches.Count > 0)
        {
            IsBusy = false;
            return;
        }
        else
        {
            m_potentialMatch = m_board.GetPotentialMatches();
            if (m_potentialMatch.Count > 0)
            {
                IsBusy = false;

                m_timeAfterFill = 0f;
            }
            else
            {
                //StartCoroutine(RefillBoardCoroutine());
                StartCoroutine(ShuffleBoardCoroutine());
            }
        }
    }

    private List<Cell> GetMatches(Cell cell)
    {
        List<Cell> listHor = m_board.GetHorizontalMatches(cell);
        if (listHor.Count < m_gameSettings.MatchesMin)
        {
            listHor.Clear();
        }

        List<Cell> listVert = m_board.GetVerticalMatches(cell);
        if (listVert.Count < m_gameSettings.MatchesMin)
        {
            listVert.Clear();
        }

        return listHor.Concat(listVert).Distinct().ToList();
    }

    private void CollapseMatches(List<Cell> matches, Cell cellEnd)
    {
        IsBusy = true;

        for (int i = 0; i < matches.Count; i++)
        {
            matches[i].ExplodeItem();
        }

        if(matches.Count > m_gameSettings.MatchesMin)
        {
            m_board.ConvertNormalToBonus(matches, cellEnd);
        }

        StartCoroutine(ShiftDownItemsCoroutine());
    }

    private IEnumerator ShiftDownItemsCoroutine()
    {
        m_board.ShiftDownItems();

        yield return new WaitForSeconds(0.2f);

        m_board.FillGapsWithNewItems();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator RefillBoardCoroutine()
    {
        m_board.ExplodeAllItems();

        yield return new WaitForSeconds(0.2f);

        m_board.Fill();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator ShuffleBoardCoroutine()
    {
        m_board.Shuffle();

        yield return new WaitForSeconds(0.3f);

        FindMatchesAndCollapse();
    }

    public void AutoWin()
    {
        if (m_gameOver || IsBusy)
            return;

        StartCoroutine(AutoWinCoroutine());
    }
    private IEnumerator AutoWinCoroutine()
    {
        IsBusy = true;

        while (!m_board.IsEmpty())
        {
            Cell focusCell = null;

            // tìm ô đầu tiên trên Board còn chứa Item làm gốc
            for (int i = 0; i < this.transform.childCount; i++)
            {
                Cell cell = this.transform.GetChild(i).GetComponent<Cell>();

                if (cell != null && !cell.IsEmpty && cell.Item != null)
                {
                    focusCell = cell;
                }
            }
            if (focusCell == null || focusCell.Item == null)
                break;

            // lấy danh sách tất cả các ô trên Board có chứa Item cùng loại với ô gốc
            List<Cell> listSameTypeCells = new List<Cell>();
            for (int i = 0; i < this.transform.childCount; i++)
            {
                Cell cell = this.transform.GetChild(i).GetComponent<Cell>();

                if (cell != null && !cell.IsEmpty && cell.Item != null)
                {
                    if (cell.Item.IsSameType(focusCell.Item))
                    {
                        listSameTypeCells.Add(cell);
                    }
                }
            }

            // chuyển lần lượt các item cùng loại xuống CheckBoard
            for (int i = 0; i < 3; i++)
            {
                Cell boardCell = listSameTypeCells[i];
                Cell checkCell = GetEmptyCheckBoardCell();

                if (checkCell == null || boardCell.Item == null)
                    break;

                Item item = boardCell.Item;

                boardCell.Free();
                checkCell.Assign(item);

                SetSortingLayer(boardCell, checkCell);

                // Thực hiện animation di chuyển
                item.View.DOMove(checkCell.transform.position, 0.3f);

                yield return new WaitForSeconds(0.15f); // Khoảng thời gian giữa các lần hạ item xuống
            }

            // 4. Chờ animation hoàn tất và kích hoạt kiểm tra nổ 3 ở CheckBoard
            yield return new WaitForSeconds(0.15f);
            yield return StartCoroutine(CheckCheckBoardMatchesCoroutine());

            IsBusy = true;
        }

        IsBusy = false;

        if (m_board.IsEmpty())
        {
            m_gameManager.SetState(GameManager.eStateGame.WIN);
        }
    }
    public void AutoLose()
    {
        if (m_gameOver || IsBusy)
            return;

        StartCoroutine(AutoLoseCoroutine());
    }
    private IEnumerator AutoLoseCoroutine()
    {
        IsBusy = true;

        while (!m_board.IsEmpty())
        {
            Cell focusCell = null;

            // tìm ô đầu tiên trên Board còn chứa Item làm gốc
            for (int i = 0; i < this.transform.childCount; i++)
            {
                Cell cell = this.transform.GetChild(i).GetComponent<Cell>();

                if (cell != null && !cell.IsEmpty && cell.Item != null)
                {
                    focusCell = cell;
                }
            }
            if (focusCell == null || focusCell.Item == null)
                break;

            // lấy danh sách tất cả các ô trên Board có chứa Item cùng loại với ô gốc
            List<Cell> listDifferentTypeCells = new List<Cell>();
            for (int i = 0; i < this.transform.childCount; i++)
            {
                Cell cell = this.transform.GetChild(i).GetComponent<Cell>();

                if (cell != null && !cell.IsEmpty && cell.Item != null)
                {
                    if (!cell.Item.IsSameType(focusCell.Item))
                    {
                        listDifferentTypeCells.Add(cell);
                    }
                }
            }

            // chuyển lần lượt các item khác loại xuống CheckBoard
            for (int i = 0; i < 5; i++)
            {
                Cell boardCell = listDifferentTypeCells[i];
                Cell checkCell = GetEmptyCheckBoardCell();

                if (checkCell == null || boardCell.Item == null)
                    break;

                Item item = boardCell.Item;

                boardCell.Free();
                checkCell.Assign(item);

                SetSortingLayer(boardCell, checkCell);

                // Thực hiện animation di chuyển
                item.View.DOMove(checkCell.transform.position, 0.3f);

                yield return new WaitForSeconds(0.15f); // Khoảng thời gian giữa các lần hạ item xuống
            }

            // 4. Chờ animation hoàn tất và kích hoạt kiểm tra nổ 3 ở CheckBoard
            yield return new WaitForSeconds(0.15f);
            yield return StartCoroutine(CheckCheckBoardMatchesCoroutine());

            IsBusy = true;
        }

        IsBusy = false;

        if (m_board.IsEmpty())
        {
            m_gameManager.SetState(GameManager.eStateGame.WIN);
        }
    }

    private void SetSortingLayer(Cell cell1, Cell cell2)
    {
        if (cell1.Item != null) cell1.Item.SetSortingLayerHigher();
        if (cell2.Item != null) cell2.Item.SetSortingLayerLower();
    }

    private bool AreItemsNeighbor(Cell cell1, Cell cell2)
    {
        return cell1.IsNeighbour(cell2);
    }

    private bool IsEmptyItemCheckBoard(Cell cell2)
    {
        return cell2 != null && cell2.IsEmpty && cell2.transform.IsChildOf(checkBoardTransform);
    }

    internal void Clear()
    {
        m_board.Clear();
    }

    private void ShowHint()
    {
        m_hintIsShown = true;
        foreach (var cell in m_potentialMatch)
        {
            cell.AnimateItemForHint();
        }
    }

    private void StopHints()
    {
        m_hintIsShown = false;
        foreach (var cell in m_potentialMatch)
        {
            cell.StopHintAnimation();
        }

        m_potentialMatch.Clear();
    }
}
