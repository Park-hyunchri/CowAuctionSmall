using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CowAuctionSmall.Models.Structures
{
    public class BoardList
    {
        public List<Board>? MultiBoards { get; set; }

        public List<Logos>? LogoBoard { get; set; } = new List<Logos>();
        public string? Size { get; set; }
    }

    public class Board
    {
        public string? Name { get; set; }
        public List<int[]>? Rows { get; set; }
    }

    public class Logos
    {
        public string? Name { get; set; }
        public List<LogoRowIdx>? Rows { get; set; }
    }

    public class LogoRowIdx
    {
        public string? ID { get; set; } // ID 속성 추가
        public List<int>? Rows { get; set; } // List<int> 형식으로 변경
    }
}
