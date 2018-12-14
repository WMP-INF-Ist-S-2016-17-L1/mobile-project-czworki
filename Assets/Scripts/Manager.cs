﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Manager : MonoBehaviour
{
    public BoardModel board;
    public GameObject discGap;
    public GameObject disc;
    public Transform startPosition;

    public Sprite greenDisc;
    public Sprite orangeDisc;

    public Player playerPrefab;
    private Player playerOrange;
    private Player playerGreen;

    public PlayerColor currentPlayerColor;

    [SerializeField]
    private GameMode gameMode;

    [SerializeField]
    private bool paused = false;


    // Use this for initialization
    void Start()
    {
        Screen.orientation = ScreenOrientation.Landscape;
        Screen.SetResolution(1920, 1080, false);
        Init();
        SetPlayers();
        board.ClearModel();
    }

    public void Init()
    {
        gameMode = MenuManager.Instance.gameMode;
        this.currentPlayerColor = PlayerColor.None;
        SetPause(false); 
        GameObject[] boardDiscGaps = GameObject.FindGameObjectsWithTag("Gap");
        Debug.Log(boardDiscGaps.Length);

        for (int x = 0; x < board.rows; x++)
        {
            for (int y = 0; y < board.columns; y++)
            {
                // Ustaw pozycje miejsc na dyski
                boardDiscGaps[TwoDimensionToOneDimension(board.columns, x, y)].GetComponent<Transform>().position = new Vector3(startPosition.position.x + y * 4, startPosition.position.y - x * 3 + 0.35f, 0);
                //Debug.Log(boardDiscGaps[TwoDimensionToOneDimension(7, x, y)].GetComponent<Transform>().position);    
            }
        }

    }
    
    public void SetPlayers()
    {
        playerOrange = Instantiate(playerPrefab);
        playerGreen = Instantiate(playerPrefab);

        playerOrange.SetPlayerColor(PlayerColor.Orange);
        playerGreen.SetPlayerColor(PlayerColor.Green);

        currentPlayerColor = playerOrange.GetPlayerColor();
        GetComponent<UIController>().ChangeNextPlayerSprite(GetSprite(currentPlayerColor));

        if (gameMode.Equals(GameMode.OneVsComputer))
        {
            playerOrange.SetActive(true);
            playerGreen.SetActive(false);   
        }
    }

    public void ChangeCurrentPlayer()
    {   
        if (currentPlayerColor.Equals(playerOrange.GetPlayerColor()))
        {
            currentPlayerColor = playerGreen.GetPlayerColor();
        }
        else
        {
            currentPlayerColor = playerOrange.GetPlayerColor();
        }

        GetComponent<UIController>().ChangeNextPlayerSprite(GetSprite(currentPlayerColor));

        playerOrange.SetActive(!playerOrange.IsActive());
        playerGreen.SetActive(!playerOrange.IsActive());

        if (gameMode.Equals(GameMode.OneVsComputer) && playerGreen.IsActive())
        {
            ComputerMove();
        }
    }

    public bool isPaused()
    {
        return paused;
    }

    public void SetPause(bool pause)
    {
        this.paused = pause;
    }

    public GameMode getGameMode()
    {
        return this.gameMode;
    }

    public int TwoDimensionToOneDimension(int cols, int row, int col)
    {
        return row * cols + col;
    }

    public int MapPlayerColorToInt(PlayerColor color)
    {
        switch (color)
        {
            case PlayerColor.None: return 0;
            case PlayerColor.Orange: return 1;
            case PlayerColor.Green: return 2;
            default: return 0;
        }
    }

    public PlayerColor MapIntToPlayerColor(int x)
    {
        switch (x)
        {
            case 0: return PlayerColor.None;
            case 1: return PlayerColor.Orange;
            case 2: return PlayerColor.Green;
            default: return PlayerColor.None;
        }
    }

    public Sprite GetSprite(PlayerColor color)
    {
        if (color.Equals(PlayerColor.Green))
            return greenDisc;
        else
            return orangeDisc;
    }

    public void SetDisc(int columnNumber)
    {
        // Jezeli gramy z komputerem i nie jest nasza kolej to nie obslugujemy klikniecia
        if (gameMode.Equals(GameMode.OneVsComputer) && playerOrange.IsActive() == false)
            return;

        Debug.Log("Ustawiam dysk!");
        Vector2Int coord = this.board.UpdateModel(columnNumber, MapPlayerColorToInt(currentPlayerColor));

        if (coord.x != (-1))
            RenderDisc(coord.x, coord.y);
        else
            return;

        
        ComputeAndHandleWinCondition();

        ChangeCurrentPlayer();
    }

    // renderowanie do poprawy
    public void RenderDisc(int row, int col)
    {
        GameObject[] boardDiscGaps = GameObject.FindGameObjectsWithTag("Gap");
        GameObject go = Instantiate(disc,
            boardDiscGaps[TwoDimensionToOneDimension(board.columns, row, col)].GetComponent<Transform>().position,
            Quaternion.identity);
        go.GetComponent<DiscController>().SetSprite(GetSprite(currentPlayerColor));
    }

    // obsługa ruchu komputera
    private void ComputerMove()
    {
        Debug.Log("Komputer wykonuje ruch!!!");
        Debug.Log("Ustawiam dysk!");

        Vector2Int coord;

        int column = ComputeNextMove();

        coord = RawSetDisc(column, playerGreen.GetPlayerColor());

        //coord = RawSetDisc(Mathf.RoundToInt(Random.Range(0, board.columns - 1)));

        RenderDisc(coord.x, coord.y);

        ComputeAndHandleWinCondition();

        ChangeCurrentPlayer();
    }

    public void ComputeAndHandleWinCondition()
    {
        if (board.CheckWinCondition(MapPlayerColorToInt(currentPlayerColor)))
        {
            Debug.Log(string.Format("Gracz {0} wygral!", currentPlayerColor));
            SetPause(true);
            GetComponent<UIController>().ShowWinInfo(currentPlayerColor.ToString());
        }
        else
        {
            if (board.coverage == board.rows * board.columns)
            {
                Debug.Log(string.Format("Remis"));
                SetPause(true);
                Debug.Break();

                GetComponent<UIController>().ShowWinInfo("Remis");
            }
        }
    }

    public int ComputeNextMove()
    {
        Vector2Int coord;
        bool IsWinMove = false;
        bool IsPlayerWinMove = false;
        int column;

        // czy jest mozliwosc wygrania gry przez komputer
        for (column = 0; column < board.columns; column++)
        {
            coord = RawSetDisc(column, PlayerColor.Green);

            if (coord.x != (-1))
            {
                IsWinMove = board.CheckWinCondition(MapPlayerColorToInt(PlayerColor.Green));

                if (IsWinMove)
                {
                    board.PopFromColumn(column);
                    return column;
                }
                else
                {
                    board.PopFromColumn(column);
                }
            }
            else
            {
                continue;
            }
        }

        // czy jest mozliwosc wygrania gry przez gracza
        for( column = 0; column < board.columns; column++ )
        {
            coord = RawSetDisc(column, PlayerColor.Orange);

            if (coord.x != (-1))
            {
                IsPlayerWinMove = board.CheckWinCondition(MapPlayerColorToInt(PlayerColor.Orange));

                if (IsPlayerWinMove)
                {
                    board.PopFromColumn(column);
                    return column;
                }
                else
                {
                    board.PopFromColumn(column);
                }
            }
            else
            {
                continue;
            }
        }

        column = 0;

        while(true)
        {
            column = Mathf.RoundToInt(Random.Range(0, board.columns - 1));
            
            coord = RawSetDisc(column, playerGreen.GetPlayerColor());

            if( coord.x != (-1))
            {
                board.PopFromColumn(column);
                return column;
            }
        }
    }

    public Vector2Int RawSetDisc(int columnNumber, PlayerColor playerColor)
    {
        return board.UpdateModel(columnNumber, MapPlayerColorToInt(playerColor));
    }
}
