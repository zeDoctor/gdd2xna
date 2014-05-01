﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace gdd2xna
{
    public class Player
    {
        /// <summary>
        /// The locations to draw the grid at for each player.
        /// </summary>
        private static readonly int[][] GRID_LOCATIONS = {new int[] {100, 100}, new int[] {800, 100}};

        /// <summary>
        /// The locations to draw the grid at for each player in small window mode.
        /// </summary>
        private static readonly int[][] GRID_LOCATIONS_SMALL = { new int[] { 0, 0 }, new int[] { 600, 0 } };
        
        /// <summary>
        /// The points per tile.
        /// </summary>
        private static readonly int POINTS_PER_TILE = 3; // Need a good value for this

        /// <summary>
        /// The game instance.
        /// </summary>
        private readonly Game1 game;

        /// <summary>
        /// The player index.
        /// </summary>
        private readonly int index;

        /// <summary>
        /// The grid of the player.
        /// </summary>
        private readonly Grid grid;

        /// <summary>
        /// The scores of the player.
        /// </summary>
        private Dictionary<TileType, int> scores = new Dictionary<TileType,int>();

        /// <summary>
        /// The sound manager instance.
        /// </summary>
        private readonly SoundManager soundManager;

        /// <summary>
        /// The scores instance.
        /// </summary>
        private readonly Scores scoreBars;

        /// <summary>
        /// The current stage of the player in the game.
        /// </summary>
        public GameStep step  { get; set; }

        /// <summary>
        /// The index of the player.
        /// </summary>
        public int Index { get { return index; } }

        /// <summary>
        /// The draw location of the board.
        /// </summary>
        private int[] location;

        /// <summary>
        /// ?
        /// </summary>
        private int[] prevSwap;

        /// <summary>
        /// ?
        /// </summary>
        private int lowestEmp = 0;

        /// <summary>
        /// Creates a new Player.
        /// </summary>
        /// <param name="game">The game instance.</param>
        /// <param name="index">The index of the player.</param>
        /// <param name="soundManager">The sound manager.</param>
        /// <param name="scores">The scores instance.</param>
        public Player(Game1 game, int index, SoundManager soundManager, Scores scoreBars)
        {
            this.game = game;
            this.index = index;
            this.soundManager = soundManager;
            this.scoreBars = scoreBars;
            if (game.SizeMode == Game1.SIZE_SMALL)
            {
                location = GRID_LOCATIONS_SMALL[index];
            }
            else
            {
                location = GRID_LOCATIONS[index];
            }
            
            this.grid = new Grid(8, 8, location[0], location[1], game);

            // Populate the scores dictionary
            foreach (TileType next in Enum.GetValues(typeof(TileType)))
            {
                // No sammiches!
                if (next == TileType.Emp)
                    continue;

                this.scores.Add(next, 0);
            }

            step = GameStep.Waiting;
            prevSwap = new int[2] { 0, 0 };
        }

        /// <summary>
        /// Update the player.
        /// </summary>
        /// <param name="gameTime">The current game time.</param>
        public void Update(GameTime gameTime)
        {
            if (!grid.Animating())
            {
                if (step == GameStep.Input)
                {
                    if (Input.LeftClick())
                    {
                        var mousePos = Input.MousePos();
                        var gridPos = grid.ScreenToGrid(mousePos.X, mousePos.Y);
                        var gridNum = grid.RCtoN(gridPos[0], gridPos[1]);
                        //example of how to play a soundEffect in the code
                        //soundManager.Play(SoundEffectName.Match3);
                        if (!grid.HasActiveSelection())
                        {
                            grid.UpdateSelection(gridPos[0], gridPos[1]);
                        }
                        else
                        {
                            var selectNum = grid.RCtoN(grid.selection[0], grid.selection[1]);
                            var swaps = grid.GetSwaps(selectNum);
                            if (swaps.Contains(gridNum))
                            {
                                grid.Swap(selectNum, gridNum);
                                prevSwap[0] = selectNum;
                                prevSwap[1] = gridNum;
                                grid.ClearSelection();
                                step = GameStep.CheckMatch;
                            }
                            else
                            {
                                grid.UpdateSelection(gridPos[0], gridPos[1]);
                            }
                        }
                    }
                }
                else if (step == GameStep.CheckMatch)
                {
                    var matches = grid.FindMatches();
                    if (matches.Count == 0)
                    {
                        grid.Swap(prevSwap[0], prevSwap[1]);
                        step = GameStep.Input;
                    }
                    else
                    {

                        bool win = false;
                        foreach (var match in matches)
                        {
                            foreach (var tile in match)
                            {
                                TileType type = grid[tile].type;
                                scores[type] += POINTS_PER_TILE;
                                win = win || scoreBars.add(type, index, POINTS_PER_TILE);
                            }
                                
                            grid.EmptyTiles(match);
                        }

                        lowestEmp = grid.DropEmpties();
                        grid.RefillBoard(lowestEmp);
                        if (win)
                        {
                            step = GameStep.Win;
                        }
                        else
                        {
                            if (grid.FindMatches().Count > 0)
                                step = GameStep.CheckMatch;
                            else
                                step = GameStep.CheckDeadlock;
                        }
                    }
                }
                else if (step == GameStep.CheckDeadlock)
                {
                    if (grid.Deadlocked())
                        grid.ShuffleBoard();
                    else
                    {
                        step = GameStep.Complete;
                        grid.ClearSelection();
                    }
                }
                else if (step == GameStep.Waiting)
                {

                }
            }
            else
            {
                for (int i = 0; i < grid.rows * grid.cols; i++)
                {
                    grid[i].update();
                }
            }
        }

        /// <summary>
        /// Draw the player (their grid).
        /// </summary>
        /// <param name="gameTime">The current game time.</param>
        /// <param name="spriteBatch">The sprite batch.</param>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch, SpriteFont font)
        {
            grid.Draw(spriteBatch, step == GameStep.Waiting);

            //spriteBatch.DrawString(font, "Green", new Vector2(10, 10), new Color(50, 50, 50));

            string text = null;
            Color? color = null;

            if (step == GameStep.Win)
            {
                text = "You win!";
                color = Color.Green;
            }
            else if (step == GameStep.Lose)
            {
                text = "Better luck next time...";
                color = Color.Red;
            }

            if (text != null)
            {
                const int BOARD_SIZE = 400;
                int x = location[0];
                int y = location[1] + BOARD_SIZE;
                Vector2 size = font.MeasureString(text);
                spriteBatch.DrawString(font, text, new Vector2(x + (BOARD_SIZE / 2) - (size.X / 2), y), color.Value);
            }
        }
    }
}
