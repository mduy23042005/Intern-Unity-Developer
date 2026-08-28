using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelGame : MonoBehaviour,IMenu
{
    public Text LevelConditionView;

    [SerializeField] private Button btnPause;
    [SerializeField] private Button btnAutoWin;
    [SerializeField] private Button btnAutoLose;

    private UIMainManager m_mngr;
    private BoardController m_boardController;

    private void Awake()
    {
        btnPause.onClick.AddListener(OnClickPause);
        btnAutoWin.onClick.AddListener(OnClickAutoWin);
        btnAutoLose.onClick.AddListener(OnClickAutoLose);
    }

    private void Update()
    {
        if (m_boardController == null)
            m_boardController = GameObject.Find("BoardController").GetComponent<BoardController>();
    }

    private void OnClickPause()
    {
        m_mngr.ShowPauseMenu();
    }

    private void OnClickAutoWin()
    {
        if (m_boardController != null)
        {
            m_boardController.AutoWin();
        }
    }
    private void OnClickAutoLose()
    {
        if (m_boardController != null)
        {
            m_boardController.AutoLose();
        }
    }

    public void Setup(UIMainManager mngr)
    {
        m_mngr = mngr;
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
