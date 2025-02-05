﻿using BattleshipsCore.Data;
using BattleshipsCore.Game.GameGrid;
using BattleshipsCore.Game.PlaceableObjects;
using BattleshipsCoreClient.Extensions;
using BattleshipsCoreClient.Iterator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipsCoreClient.PlacementFormComponents
{
    public class TileGrid : ITileAggregate
    {
        private TableLayoutPanel _panel;

        private GameMapData? _originalMapData;
        private Tile[,]? _currentGrid;
        public Vec2 GridSize => new(_currentGrid!.GetLength(1), _currentGrid!.GetLength(0));

        public TileGrid(TableLayoutPanel panel)
        {
            _panel = panel;
        }

        public void Initialize(
            GameMapData originalMapData,
            EventHandler hoverAction,
            EventHandler clickAction)
        {
            _originalMapData = originalMapData;
            _currentGrid = originalMapData.Grid.Clone() as Tile[,];

            var rows = _currentGrid!.GetLength(0);
            var columns = _currentGrid.GetLength(1);

            _panel.ColumnCount = columns;
            _panel.RowCount = rows;

            _panel.Controls.Clear();
            _panel.ColumnStyles.Clear();
            _panel.RowStyles.Clear();

            for (int i = 0; i < columns; i++)
            {
                _panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100 / columns));
            }

            for (int i = 0; i < rows; i++)
            {
                _panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100 / rows));
            }

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    var tile = _currentGrid[i, j];
                    var button = new Button
                    {
                        Name = $"{i}_{j}",
                        BackColor = tile.Type.ToColor(),
                        Dock = DockStyle.Fill,
                        Padding = new Padding(0),
                        Margin = new Padding(0)
                    };

                    button.MouseHover += hoverAction;
                    button.Click += clickAction;

                    _panel.Controls.Add(button, j, i);
                }
            }
        }

        public bool IsPlaceable(Vec2 position, PlaceableObject placeableObject)
        { 
            if (_currentGrid![position.X, position.Y].IsDisabled) return false;

            return placeableObject.IsPlaceable(_currentGrid, position);
        }

        public void ColorTiles(List<Vec2> tiles, Color newColor)
        {
            foreach (var item in tiles)
            {
                var selBut = _panel.GetControlFromPosition(item.Y, item.X);

                selBut.BackColor = newColor;
            }
        }

        public void UpdateTilesAccessablity()
        {
            for (int i = 0; i < RowCount; i++) {

                for (int j = 0; j < ColumnCount; j++)
                {
                    if (_currentGrid![j, i].IsDisabled)
                    {
                        _panel.GetControlFromPosition(i, j).Text = "x";
                    }
                    else {
                        _panel.GetControlFromPosition(i, j).Text = "";
                    }
                }
            }
        }

        public void RestoreTileColor(List<Vec2> tiles)
        {
            foreach (var item in tiles)
            {
                var selBut = _panel.GetControlFromPosition(item.Y, item.X);

                var originalColor = _originalMapData!.Grid[item.X, item.Y].Type.ToColor();

                selBut.BackColor = originalColor;
            }
        }

        public List<TileUpdate> SetTiles(List<Vec2> tiles, TileType newType)
        {
            var updates = tiles.Select(x => new TileUpdate(x, newType));

            //var previousStates = new List<TileUpdate>(tiles.Count);

            //foreach (var tile in tiles)
            //{
            //    previousStates.Add(new TileUpdate(tile, _currentGrid![tile.X, tile.Y].Type));

            //    _currentGrid![tile.X, tile.Y].Type = newType;
            //}

            //return previousStates;

            return SetTiles(updates);
        }

        public List<TileUpdate> SetTiles(IEnumerable<TileUpdate> updates)
        {
            var previousStates = new List<TileUpdate>();

            foreach (var update in updates)
            {
                var (x, y) = update.TilePosition;

                previousStates.Add(new TileUpdate(update.TilePosition, _currentGrid![x, y].Type));

                _currentGrid![x, y].Type = update.NewType;

                var selectedButton = _panel.GetControlFromPosition(y, x);
                selectedButton.BackColor = update.NewType.ToColor();
            }

            return previousStates;
        }

        public int RowCount
        {
            get { return _currentGrid!.GetLength(0); }
        }

        public int ColumnCount
        {
            get { return _currentGrid!.GetLength(1); }
        }

        public Tile this[int i, int j]
        {
            get { return _currentGrid![i, j]; }
        }

        public override ITileIterator CreateIterator()
        {
            return new GameTileIterator(this);
        }
    }
}
