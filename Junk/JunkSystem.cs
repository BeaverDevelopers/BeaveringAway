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

	static readonly Texture2D LogTexture = GD.Load<Texture2D>("res://Junk/sprites/Log.png");

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

		var sprite = new Sprite2D();
		sprite.Texture = LogTexture;
		sprite.ZIndex = 2;

		var offset = new Vector2((_rng.NextSingle() - 0.5f) * 8f, (_rng.NextSingle() - 0.5f) * 8f);
		var worldPos = _waterLayer.MapToLocal(spawnTile) + offset;
		sprite.Position = worldPos;
		_waterLayer.AddChild(sprite);

		_items.Add(new JunkItem
		{
			Type = JunkType.Log,
			WorldPos = worldPos,
			Velocity = Vector2.Zero,
			Sprite = sprite,
		});
	}

	public void Update(Terrain terrain)
	{
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
					_items[i].Sprite.Position = _items[i].WorldPos;
					_items[j].Sprite.Position = _items[j].WorldPos;
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
			float fx = terrain.Tiles[tx, ty].SmoothedFlowX;
			float fy = terrain.Tiles[tx, ty].SmoothedFlowY;
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
					WaterVelocity.Down  => (0f, 1f),
					WaterVelocity.Up    => (0f, -1f),
					WaterVelocity.Left  => (-1f, 0f),
					WaterVelocity.Right => (1f, 0f),
					_ => (0f, 0f),
				};
				if (dirX == 0f && dirY == 0f)
					continue;
			}

			float depth = terrain.Tiles[tx, ty].WaterHeight;
			float speed = MathF.Min(depth, 20) * DriftSpeed;
			var desired = new Vector2(dirX * speed, dirY * speed);

			// Smooth velocity toward desired (inertia eliminates jitter)
			item.Velocity = item.Velocity.Lerp(desired, Inertia);

			var newPos = item.WorldPos + item.Velocity;
			var newTile = _waterLayer.LocalToMap(newPos);

			// Dam collision with deflection
			if (IsDam(terrain, newTile.X, newTile.Y))
			{
				var slideH = item.WorldPos + new Vector2(item.Velocity.X, 0);
				var slideV = item.WorldPos + new Vector2(0, item.Velocity.Y);
				var tileH = _waterLayer.LocalToMap(slideH);
				var tileV = _waterLayer.LocalToMap(slideV);

				if (MathF.Abs(item.Velocity.Y) >= MathF.Abs(item.Velocity.X) && !IsDam(terrain, tileV.X, tileV.Y))
					newPos = slideV;
				else if (!IsDam(terrain, tileH.X, tileH.Y))
					newPos = slideH;
				else if (!IsDam(terrain, tileV.X, tileV.Y))
					newPos = slideV;
				else
				{
					item.Velocity = Vector2.Zero;
					continue;
				}
			}

			item.WorldPos = newPos;
			item.Sprite.Position = newPos;
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
		_items[index].Sprite?.QueueFree();
		_items.RemoveAt(index);
	}

	public void ClearAll()
	{
		for (int i = _items.Count - 1; i >= 0; i--)
			Remove(i);
	}
}
