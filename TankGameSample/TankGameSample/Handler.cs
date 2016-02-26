using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace TankGameSample
{
    class Handler
    {
        public Tank[] tanks;
        public List<Obstacle> obstacles;
        public Client client;
        public String serverReply;
        public List<Coins> coinPiles;
        public List<LifePack> lifePacks;
        public String indicator;
        public String nextMove;
        public Box[,] grid2;
        public Vector2 position;
        public int playerNum;
        public List<Bullet> bullets;
        public MyPathNode[,] grid;


        public Handler()
        {
            client = new Client();
            obstacles = new List<Obstacle>();
            coinPiles = new List<Coins>();
            lifePacks = new List<LifePack>();
            grid2 = new Box[11, 11];
            bullets = new List<Bullet>();
            grid = new MyPathNode[10, 10];
            serverReply = null;
        }

        public Boolean join(Rectangle aScreen)
        {
            this.send("JOIN#");
            return this.receive(aScreen);
        }
        public void createTanks(Rectangle gameScreen)
        {
            Color[] colors = new Color[] { Color.Blue, Color.Green, Color.Red, Color.Yellow, Color.Magenta };

            String[] players = serverReply.Split(':');
            tanks = new Tank[players.Length - 1];
            for (int i = 1; i < players.Length; i++)
            {
                String[] playerParts = players[i].Split(';');
                String[] playerPos = playerParts[1].Split(',');
                tanks[i - 1] = new Tank(players[i].ElementAt(1) - 48, colors[i - 1], new Vector2(int.Parse(playerPos[0]), int.Parse(playerPos[1])), players[i].ElementAt(players[i].Length - 1) - 48);
            }
        }

        public Boolean receive(Rectangle aScreen)
        {
            String reply = this.client.receive();
            Console.WriteLine(reply);
            indicator = reply.Substring(0, 2);
            if (indicator == "I:" || indicator == "G:" || indicator == "S:" || indicator == "C:" || indicator == "L:")
            {
                serverReply = reply;
                return this.processReply(aScreen);
            }
            else if (reply == "GAME_FINISHED")
            {
                //finsih game
                return false;
            }
            else
            {
                return false;
            }
        }

        public Boolean processReply(Rectangle aScreen)
        {
            indicator = serverReply.Substring(0, 2);
            if (indicator == "I:")
            {
                playerNum = serverReply.ElementAt<char>(serverReply.IndexOf('P') + 1) - 48;
                createObstacles();
            }
            else if (indicator == "G:")
            {
                update();
            }
            else if (indicator == "S:")
            {
                createTanks(aScreen);
            }
            else if (indicator == "C:")
            {
                createCoinPile();
            }
            else
            {
                createLifePack();
            }
            return true;
        }

        public void createCoinPile()
        {
            String[] parts = serverReply.Split(':');
            String[] pos = parts[1].Split(',');
            coinPiles.Add(new Coins(int.Parse(parts[2]), int.Parse(parts[3]), new Vector2(int.Parse(pos[0]), int.Parse(pos[1]))));

        }



        public int updateCoinPile(int aTime)
        {
            int count = coinPiles.Count;
            for (int i = 0; i < count; i++)
            {
                Coins pile = coinPiles.ElementAt(i);
                if (pile != null)
                {
                    pile.spentTime += aTime;
                    if (pile.spentTime >= pile.lifeTime)
                    {
                        pile.isAlive = false;
                        coinPiles.Remove(pile);
                        count--;
                    }
                    for (int j = 0; j < tanks.Length; j++)
                    {
                        if (tanks[j].position == pile.position)
                        {
                            pile.isAlive = false;
                            coinPiles.Remove(pile);
                            count--;
                        }
                    }
                }
            }
            return coinPiles.Count;
        }

        public void killTank(int i)
        {
            this.tanks[i].status = 1;

        }

        public int updateLifePacks(int aTime)
        {
            int count = lifePacks.Count;
            for (int i = 0; i < count; i++)
            {
                LifePack pack = lifePacks.ElementAt(i);
                if (pack != null)
                {
                    pack.spentTime += aTime;
                    if (pack.spentTime >= pack.lifeTime)
                    {
                        pack.isAlive = false;
                        lifePacks.Remove(pack);
                        count--;
                    }
                    for (int j = 0; j < tanks.Length; j++)
                    {
                        if (tanks[j].position == pack.position)
                        {
                            pack.isAlive = false;
                            lifePacks.Remove(pack);
                            count--;
                        }
                    }
                }
            }
            return lifePacks.Count;
        }


        public void createLifePack()
        {
            String[] parts = serverReply.Split(':');
            String[] pos = parts[1].Split(',');
            lifePacks.Add(new LifePack(int.Parse(parts[2]), new Vector2(int.Parse(pos[0]), int.Parse(pos[1]))));
        }

        public void moveTank(String move)
        {
            client.send(move);
            client.receive();
        }


        public void shoot()
        {
            client.send("SHOOT#");
            client.receive();
        }

        public void moveTank()
        {
            this.findPath();
            this.findNext();
            client.send(nextMove);
            client.receive();
        }

        public void update()
        {
            String[] parts = serverReply.Split(':');
            String[] playerParts = new String[7];
            for (int i = 1; i < parts.Length; i++)
                if (parts[i].ElementAt(0) == 'P')
                {
                    playerParts = parts[i].Split(';');
                    grid[(int)tanks[i - 1].position.X, (int)tanks[i - 1].position.Y].IsWall = false;
                    String[] positionArray = playerParts[1].Split(',');
                    Vector2 pos = new Vector2(int.Parse(positionArray[0]), int.Parse(positionArray[1])); // the position of a bullet is taken in pixels
                    tanks[i - 1].position = pos;


                    grid[(int)tanks[i - 1].position.X, (int)tanks[i - 1].position.Y] = new MyPathNode()
                    {
                        IsWall = true,
                        X = (int)tanks[i - 1].position.X,
                        Y = (int)tanks[i - 1].position.Y,
                    };


                    int dir = Int32.Parse(playerParts[2]);
                    tanks[i - 1].direction = dir;
                    if (playerParts[3] == "0")
                    {
                        tanks[i - 1].hasShot = false;
                    }
                    else
                    {
                        tanks[i - 1].hasShot = true;
                        updateBullets();
                        Vector2 bulletPosition;
                        switch (dir)
                        {
                            case 0:
                                {
                                    bulletPosition = new Vector2(pos.X * 60, pos.Y * 60 - 60);
                                    break;
                                }
                            case 1:
                                {
                                    bulletPosition = new Vector2(pos.X * 60 + 60, pos.Y * 60);
                                    break;
                                }
                            case 2:
                                {
                                    bulletPosition = new Vector2(pos.X * 60, pos.Y * 60 + 60);
                                    break;
                                }
                            case 3:
                                {
                                    bulletPosition = new Vector2(pos.X * 60 - 60, pos.Y * 60);
                                    break;
                                }
                            default:
                                {
                                    bulletPosition = new Vector2(0, 0);
                                    break;
                                }
                        }
                        bullets.Add(new Bullet(dir, bulletPosition));

                    }
                    tanks[i - 1].health = Int32.Parse(playerParts[4]);
                    tanks[i - 1].coins = Int32.Parse(playerParts[5]);
                    tanks[i - 1].points = Int32.Parse(playerParts[6]);
                    if (tanks[i - 1].health == 0)
                    {
                        if (tanks[i - 1].status == 0)
                        {
                            this.killTank(i - 1);
                        }
                        else if (tanks[i - 1].status == 1)
                        {
                            tanks[i - 1].status = 2;
                        }
                    }

                }
                else
                {
                    String[] obPos = parts[i].Split(';');
                    String[] posDir;
                    for (int j = 0; j < obPos.Length; j++)
                    {
                        posDir = obPos[j].Split(',');
                        int damageLevel = Int32.Parse(posDir[2]);
                        if (damageLevel == 100)
                        {
                            obstacles.RemoveAt(j);
                        }
                        else
                        {
                            obstacles.ElementAt(j).damageLevel = damageLevel;
                        }

                    }
                }



            //  AI code 

            /*position = tanks[playerNum].position;
            Boolean inLineX = false;
            Boolean inLineY = false;
            List<Tank> inLineXTanks = new List<Tank>();
            List<Tank> inLineYTanks = new List<Tank>();

            for (int i = 0; i < tanks.Length; i++)
            {
                if (i == playerNum || tanks[i].health == 0)
                {
                    continue;
                }
                if (position.X == tanks[i].position.X)
                {
                    inLineX = true;
                    inLineXTanks.Add(tanks[i]);
                }
                else if (position.Y == tanks[i].position.Y)
                {
                    inLineY = true;
                    inLineYTanks.Add(tanks[i]);
                }

            }
            if (inLineX == true)
            {
                foreach (Tank tank in inLineXTanks)
                {
                    if (tank.position.Y < position.Y)
                    {
                        if (tanks[playerNum].direction == 0)
                        {
                            shoot();
                        }
                        else
                        {
                            moveTank("UP#");
                        }
                    }
                    if (tank.position.Y > position.Y)
                    {
                        if (tanks[playerNum].direction == 2)
                        {
                            shoot();
                        }
                        else
                        {
                            moveTank("DOWN#");
                        }
                    }

                }
            }
            else if (inLineY == true)
            {
                foreach (Tank tank in inLineYTanks)
                {
                    if (tank.position.X < position.X)
                    {
                        if (tanks[playerNum].direction == 3)
                        {
                            shoot();
                        }
                        else
                        {
                            moveTank("LEFT#");
                        }
                    }
                    if (tank.position.X > position.X)
                    {
                        if (tanks[playerNum].direction == 1)
                        {
                            shoot();
                        }
                        else
                        {
                            moveTank("RIGHT#");
                        }
                    }

                }
            }
            else
            {
                moveTank();
            }*/
            moveTank();
        }

        public void send(String aMessage)
        {
            this.client.send(aMessage);
        }

        public void createObstacles()
        {

            if (serverReply.ElementAt<char>(0) == 'I')
            {
                ///Console.WriteLine(serverReply.IndexOf('P'));
                //Console.WriteLine(serverReply.Length);
                String positions = serverReply.Substring(serverReply.IndexOf('P') + 3, (serverReply.Length - serverReply.IndexOf('P') - 3));
                ///Console.WriteLine(positions);
                ///String[] brickPositions = new String[3];
                String[] obstaclePositions = positions.Split(':');
                String type = "brickWall";
                for (int i = 0; i < obstaclePositions.Length; i++)
                {

                    String[] posPairs = obstaclePositions[i].Split(';');
                    for (int j = 0; j < posPairs.Length; j++)
                    {
                        String[] pair = posPairs[j].Split(',');
                        Vector2 place = new Vector2(int.Parse(pair[0]), int.Parse(pair[1]));
                        obstacles.Add(new Obstacle(type, place));
                        grid[(int)place.X, (int)place.Y] = new MyPathNode()
                        {
                            IsWall = true,
                            X = (int)place.X,
                            Y = (int)place.Y,
                        };
                    }

                    if (i == 0)
                    {
                        type = "stoneWall";
                    }
                    else if (i == 1)
                    {
                        type = "water";
                    }
                    else
                    {

                    }

                }
                for (int k = 0; k < 10; k++)
                {
                    for (int l = 0; l < 10; l++)
                    {
                        if (grid[k, l] == null)
                        {
                            grid[k, l] = new MyPathNode()
                            {
                                IsWall = false,
                                X = k,
                                Y = l,
                            };
                        }
                    }

                }

            }
            else
            {
            }
        }

        public void updateBullets()
        {
            try
            {
                List<Bullet> toBeRemoved = new List<Bullet>();
                foreach (Bullet bullet in bullets)
                {
                    Boolean bulletHit = false;
                    if (bullet.direction == 0)
                    {
                        if (bullet.position.Y > 8)
                        {
                            bullet.position.Y = bullet.position.Y - 9;
                        }
                        else
                        {
                            bullet.hit = true;
                            bulletHit = true;
                        }
                    }
                    else if (bullet.direction == 1)
                    {
                        if (bullet.position.X < 562)
                        {
                            bullet.position.X = bullet.position.X + 9;
                        }
                        else
                        {
                            bullet.hit = true;
                            bulletHit = true;
                        }
                    }

                    else if (bullet.direction == 2)
                    {
                        if (bullet.position.Y < 562)
                        {
                            bullet.position.Y = bullet.position.Y + 9;
                        }
                        else
                        {
                            bullet.hit = true;
                            bulletHit = true;
                        }
                    }
                    else if (bullet.direction == 3)
                    {
                        if (bullet.position.X > 8)
                        {
                            bullet.position.X = bullet.position.X - 9;
                        }
                        else
                        {
                            bullet.hit = true;
                            bulletHit = true;
                        }
                    }
                    else
                    {
                    }
                    if (bulletHit == true)
                    {
                        toBeRemoved.Add(bullet);
                    }
                }
                foreach (Bullet bullet in toBeRemoved)
                {
                    bullets.Remove(bullet);
                }
            }
            catch (Exception e)
            {

            }
        }

        public void computeCost(int source_x, int source_y, int dest_x, int dest_y)
        {
            Box source = grid2[source_x, source_y];
            for (int k = source_x - 1; k <= source_x + 1; k = k + 2)
            {
                for (int l = source_y - 1; l <= source_y + 1; l = l + 2)
                {
                    if (k == source_x && l == source_y)
                    {
                        source.f = 1;
                        source.computed = true;
                    }
                    else
                    {
                        Box b = grid2[k, l];
                        if (b.computed == true)
                        {
                            source.f = b.f + 1;
                            source.computed = true;
                        }
                        else if (b.computing == true)
                        {

                        }
                        else
                        {
                            computeCost(k, l, dest_x, dest_y);
                        }
                    }
                }
            }
        }
      
        public void findPath()
        {
            aStar = new MySolver<MyPathNode, Object>(grid);
            try
            {
                if (tanks[playerNum].health < 70)
                {
                    if (lifePacks.ElementAt<LifePack>(0) != null)
                    {
                        path = aStar.Search(new Vector2(position.X, position.Y), lifePacks, null, tanks[playerNum].direction);
                    }
                    else
                    {
                        path = aStar.Search(new Vector2(position.X, position.Y), coinPiles, null, tanks[playerNum].direction);
                    }
                }
                else
                {
                    if (coinPiles.ElementAt<Coins>(0) == null)
                    {
                        path = aStar.Search(new Vector2(position.X, position.Y), new Vector2(9, 9), null, tanks[playerNum].direction);
                    }
                    else
                    {
                        path = aStar.Search(new Vector2(position.X, position.Y), coinPiles, null, tanks[playerNum].direction);
                    }
                }
            }
            catch (ArgumentOutOfRangeException e)
            {
                path = aStar.Search(new Vector2(position.X, position.Y), new Vector2(9, 9), null, tanks[playerNum].direction);
            }

        }
        IEnumerable<MyPathNode> path;
        MySolver<MyPathNode, Object> aStar;

        public class MySolver<TPathNode, TUserContext> : SpatialAStar<TPathNode, TUserContext> where TPathNode : IPathNode<TUserContext>
        {
            protected override Double Heuristic(PathNode inStart, PathNode inEnd)
            {
                return Math.Abs(inStart.X - inEnd.X) + Math.Abs(inStart.Y - inEnd.Y);
            }

            protected override Double NeighborDistance(PathNode inStart, PathNode inEnd, int aDirection)
            {
                return Heuristic(inStart, inEnd);
            }

            public MySolver(TPathNode[,] inGrid)
                : base(inGrid)
            {
            }
        }

        public class MyPathNode : IPathNode<Object>
        {
            public Int32 X { get; set; }
            public Int32 Y { get; set; }
            public Boolean IsWall { get; set; }

            public bool IsWalkable(object usUsed)
            {
                return !IsWall;
            }
        }

        public void findNext()
        {
            int counter = 0;
            try
            {
                foreach (MyPathNode node in path)
                {
                    if (counter == 0)
                    {
                        counter++;
                        continue;
                    }
                    Vector2 p = new Vector2(node.X, node.Y);
                    if (p.X == position.X)
                    {
                        if (p.Y == position.Y - 1)
                        {
                            nextMove = "UP#";
                        }
                        else if (p.Y == position.Y + 1)
                        {
                            nextMove = "DOWN#";
                        }
                        else
                        {
                            Console.WriteLine("Wrong algo!!!!!!!!!!!!!!" + p.Y + "\t" + position.Y);
                        }

                    }
                    else if (p.Y == position.Y)
                    {
                        if (p.X == position.X - 1)
                        {
                            nextMove = "LEFT#";
                        }
                        else if (p.X == position.X + 1)
                        {
                            nextMove = "RIGHT#";
                        }
                        else
                        {
                            Console.WriteLine("Wrong algo!!!!!!!!!!!!!!" + p.X + "\t" + position.X);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Wrong algo!!!!!!!!!!!!!!" + p.X + "\t" + position.X + "\t" + p.Y + "\t" + position.Y);
                    }
                    break;
                }
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("No path found!!!");
                path = aStar.Search(new Vector2(position.X, position.Y), new Vector2(9, 9), null, tanks[playerNum].direction);
                this.findNext();
            }
        }
    }
}

