using Godot;
using System;
using System.Collections.Generic;

public class JunkSystem
{
	readonly List<JunkItem> _items = new();
	readonly Random _rng = new();
	TileMapLayer _waterLayer;
	float _tileSize;

	public int MaxItems = 5;
	public int SpawnInterval = 100;
	public float DriftSpeed = 0.35f;   // px/tick per depth unit. depth=20 → 7 px/tick
	const float FlowThreshold = 0.3f;
	const float Inertia = 0.12f;      // lerp factor toward desired velocity (lower = smoother)
	const float SeparationDist = 12f;
	const float SeparationForce = 0.08f;
	public float GravityBias = 1.0f;
	public int MaxLifetime = 3000;

	static readonly PackedScene LogScene = GD.Load<PackedScene>("res://scenes/log.tscn");

	public int Count => _items.Count;

	public void Initialize(TileMapLayer waterLayer)
	{
		_waterLayer = waterLayer;
		_tileSize = waterLayer.TileSet.TileSize.X;
	}

	public void Spawn(Terrain terrain, Vector2I spawnTile, int tick)
	{
		if (tick % SpawnInterval != 0) return;
		if (_items.Count >= MaxItems) return;
		if (terrain.TileWater(spawnTile.X, spawnTile.Y) == 0) return;

		var node = LogScene.Instantiate<Node2D>();
		node.ZIndex = 2;

		var offset = new Vector2((_rng.NextSingle() - 0.5f) * 8f, (_rng.NextSingle() - 0.5f) * 8f);
		var worldPos = _waterLayer.MapToLocal(spawnTile) + offset;
		node.Position = worldPos;
		_waterLayer.AddChild(node);

		_items.Add(new JunkItem
		{
			Type = JunkType.Log,
			WorldPos = worldPos,
			Velocity = Vector2.Zero,
			Node = node,
			SpawnTick = tick,
		});
	}

	public void Update(Terrain terrain, int tick)
	{
		// Remove items whose node was freed externally (e.g. player pickup)
		for (int i = _items.Count - 1; i >= 0; i--)
		{
			if (!GodotObject.IsInstanceValid(_items[i].Node))
			{
				_items.RemoveAt(i);
				continue;
			}
			if (tick - _items[i].SpawnTick > MaxLifetime)
			{
				Remove(i);
				continue;
			}
		}

		// Gentle separation so items don't pile up
		for (int i = 0; i < _items.Count; i++)
		{
			for (int j = i + 1; j < _items.Count; j++)
			{
				var diff = _items[i].WorldPos - _items[j].WorldPos;
				float dist = diff.Length();
				if (dist < SeparationDist && dist > 0.001f)
				{
					var push = diff / dist * (SeparationDist - dist) * SeparationForce;
					_items[i].WorldPos += push;
					_items[j].WorldPos -= push;
					_items[i].Node.Position = _items[i].WorldPos;
					_items[j].Node.Position = _items[j].WorldPos;
				}
			}
		}

		for (int i = _items.Count - 1; i >= 0; i--)
		{
			var item = _items[i];
			var tilePos = _waterLayer.LocalToMap(item.WorldPos);
			int tx = tilePos.X, ty = tilePos.Y;

			if (tx < 0 || ty < 0 || tx >= terrain.Columns || ty >= terrain.Rows)
			{
				Remove(i);
				continue;
			}

			if (terrain.Tiles[tx, ty].WaterHeight == 0)
			{
				// Stranded: bleed off velocity
				item.Velocity *= 0.9f;
				if (item.Velocity.Length() < 0.01f)
					item.Velocity = Vector2.Zero;
				continue;
			}

			// Desired direction from flow field
			float fx = terrain.WaterTiles[tx, ty].SmoothedFlowX;
			float fy = terrain.WaterTiles[tx, ty].SmoothedFlowY;
			float flowLen = MathF.Sqrt(fx * fx + fy * fy);

			float dirX, dirY;
			if (flowLen >= FlowThreshold)
			{
				dirX = fx / flowLen;
				dirY = fy / flowLen;
			}
			else
			{
				var vel = terrain.Tiles[tx, ty].WaterVelocity;
				(dirX, dirY) = vel switch
				{
					WaterDirection.Down  => (0f, 1f),
					WaterDirection.Up    => (0f, -1f),
					WaterDirection.Left  => (-1f, 0f),
					WaterDirection.Right => (1f, 0f),
					_ => (0f, 0f),
				};
			}

			float depth = terrain.Tiles[tx, ty].WaterHeight;
			float speed = MathF.Min(depth, 20) * DriftSpeed;
			var desired = new Vector2(dirX * speed, dirY * speed);

			// Gravity: always pull downward in water
			desired.Y += GravityBias;

			// Smooth velocity toward desired (inertia eliminates jitter)
			item.Velocity = item.Velocity.Lerp(desired, Inertia);

			var newPos = item.WorldPos + item.Velocity;
			var newTile = _waterLayer.LocalToMap(newPos);

			// Collision handling with gravity-aware deflection
			if (IsDam(terrain, newTile.X, newTile.Y))
			{
				var slideH = item.WorldPos + new Vector2(item.Velocity.X, 0);
				var slideV = item.WorldPos + new Vector2(0, item.Velocity.Y);
				var tileH = _waterLayer.LocalToMap(slideH);
				var tileV = _waterLayer.LocalToMap(slideV);
				bool canH = !IsDam(terrain, tileH.X, tileH.Y);
				bool canV = !IsDam(terrain, tileV.X, tileV.Y);

				if (canV && canH)
					newPos = MathF.Abs(item.Velocity.Y) >= MathF.Abs(item.Velocity.X) ? slideV : slideH;
				else if (canV)
					newPos = slideV;
				else if (canH)
					newPos = slideH;
				else
				{
					// Blocked on both axes — nudge sideways to find a way around
					if (item.SlideBias == 0)
						item.SlideBias = _rng.Next(2) == 0 ? -1 : 1;

					float nudge = GravityBias;
					var tryPref = item.WorldPos + new Vector2(item.SlideBias * nudge, 0);
					var tryOpp  = item.WorldPos + new Vector2(-item.SlideBias * nudge, 0);
					var tilePref = _waterLayer.LocalToMap(tryPref);
					var tileOpp  = _waterLayer.LocalToMap(tryOpp);

					if (!IsDam(terrain, tilePref.X, tilePref.Y))
						newPos = tryPref;
					else if (!IsDam(terrain, tileOpp.X, tileOpp.Y))
					{
						item.SlideBias = -item.SlideBias;
						newPos = tryOpp;
					}
					else
					{
						item.Velocity = Vector2.Zero;
						continue;
					}
				}
			}
			else
			{
				// Path clear — reset slide bias so gravity pulls straight down
				item.SlideBias = 0;
			}

			item.WorldPos = newPos;
			item.Node.Position = newPos;
		}
	}

	static bool IsDam(Terrain terrain, int x, int y)
	{
		if (x < 0 || y < 0 || x >= terrain.Columns || y >= terrain.Rows)
			return false;
		return terrain.Tiles[x, y].ObstructionHeight > 0;
	}

	void Remove(int index)
	{
		_items[index].Node?.QueueFree();
		_items.RemoveAt(index);
	}

	public void ClearAll()
	{
		for (int i = _items.Count - 1; i >= 0; i--)
			Remove(i);
	}
}
