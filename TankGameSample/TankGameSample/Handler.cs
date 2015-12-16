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
        public Box[,] grid2;
        public Vector2 position;
        public int playerNum;
        public List<Bullet> bullets;


        public Handler()
        {
            client = new Client();
            obstacles = new List<Obstacle>();
            coinPiles = new List<Coins>();
            lifePacks = new List<LifePack>();
            grid2 = new Box[10, 10];
            bullets = new List<Bullet>();
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
            else
            {
                return false;
            }
            //return reply;
        }

        public Boolean processReply(Rectangle aScreen)
        {
            indicator = serverReply.Substring(0, 2);
            if (indicator == "I:" || indicator == "G:" || indicator == "S:" || indicator == "C:" || indicator == "L:")
            {
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
            else
            {
                return false;
            }
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

        public void killTank(int i){
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

        public void update()
        {
            String[] parts = serverReply.Split(':');
            String[] playerParts = new String[7];
            for (int i = 1; i < parts.Length; i++)
                if (parts[i].ElementAt(0) == 'P')
                {
                    playerParts = parts[i].Split(';');
                    String[] positionArray = playerParts[1].Split(',');
                    Vector2 pos = new Vector2(int.Parse(positionArray[0]), int.Parse(positionArray[1])); // the position of a bullet is taken in pixels
                    tanks[i - 1].position = pos;
                    
                    
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
                        else { 
                            obstacles.ElementAt(j).damageLevel = damageLevel; 
                        }

                    }
                }
            position = tanks[playerNum].position;
            Boolean inLineX = false;
            Boolean inLineY = false;
            List<Tank> inLineXTanks= new List<Tank>();
            List<Tank> inLineYTanks= new List<Tank>();

            for (int i = 0; i < tanks.Length; i++)
            {
                if (i == playerNum || tanks[i].health == 0)
                {
                    continue;
                }
                if (position.X == tanks[i].position.X && Math.Abs(position.Y - tanks[i].position.Y) < 5)
                {
                    inLineX = true;
                    inLineXTanks.Add(tanks[i]);
                }
                else if (position.Y == tanks[i].position.Y && Math.Abs(position.X - tanks[i].position.X) < 5)
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
        }

        public void send(String aMessage)
        {
            this.client.send(aMessage);
        }

        public void createObstacles()
        {

            if (serverReply.ElementAt<char>(0) == 'I')
            {
                String positions = serverReply.Substring(serverReply.IndexOf('P') + 3, (serverReply.Length - serverReply.IndexOf('P') - 3));
                String[] obstaclePositions = positions.Split(':');
                String type = "brickWall";
                for (int i = 0; i < obstaclePositions.Length; i++)
                {
                    
                    String[] posPairs = obstaclePositions[i].Split(';');
                    for (int j = 0; j < posPairs.Length;j++)
                    {
                        String[] pair = posPairs[j].Split(',');
                        Vector2 place = new Vector2(int.Parse(pair[0]), int.Parse(pair[1]));
                        obstacles.Add(new Obstacle(type, place));
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
                            bullet.hit=true;
                            bulletHit = true;
                        }
                    }
                    else if (bullet.direction == 1)
                    {
                        if (bullet.position.X <562)
                        {
                            bullet.position.X = bullet.position.X + 9;
                        }
                        else {
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
                foreach (Bullet bullet in toBeRemoved){
                    bullets.Remove(bullet);
                }

            }
            catch(Exception e)
            {
                
            }
        }
    }
}

