using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CowAuctionSmall.Models;
using CowAuctionSmall.Models.Structures;

namespace CowAuctionSmall.Models.XMLParser
{
    public class BoardXmlParser
    {
        public  BoardList ParseXml(string xmlFilePath)
        {
            var logger = NLogger.Instance;
            BoardList boardList = new BoardList();
            boardList.MultiBoards = new List<Board>();

            XDocument xmlDoc = XDocument.Load(xmlFilePath);
            var root = xmlDoc.Root;
            if (root == null)
            {
                logger.LogError($"BoardXmlParser: Root element가 없습니다. ({xmlFilePath})");
                return boardList;
            }

            var sizeElement = root.Element("Size");
            if (sizeElement != null)
            {
                boardList.Size = sizeElement.Value;
            }

            var multiBoards = root.Elements("MultiBoard");
            foreach (var multiBoard in multiBoards)
            {
                Board board = new Board();
                board.Name = multiBoard.Attribute("Name")?.Value;
                board.Rows = new List<int[]>();

                var rowIdxs = multiBoard.Elements("RowIdx");
                foreach (var rowIdx in rowIdxs)
                {
                    var rowDataStr = rowIdx.Value;
                    if (!string.IsNullOrEmpty(rowDataStr))
                    {
                        var rowData = new List<int>();
                        foreach (var token in rowDataStr.Split(','))
                        {
                            if (int.TryParse(token, out var value))
                            {
                                rowData.Add(value);
                            }
                            else
                            {
                                logger.LogWarn($"BoardXmlParser: RowIdx parse failed '{token}' in {xmlFilePath}");
                            }
                        }

                        if (rowData.Count > 0)
                        {
                            board.Rows.Add(rowData.ToArray());
                        }
                    }
                }

                boardList.MultiBoards.Add(board);
            }

            var logoBoards = root.Elements("LogoBoard");
            foreach (var logoRows in logoBoards)
            {
                Logos logos = new Logos();
                logos.Name = logoRows.Attribute("Name")?.Value;
                logos.Rows = new List<LogoRowIdx>();

                var rowIdxs = logoRows.Elements("LogoRowIdx");
                foreach (var rowIdx in rowIdxs)
                {
                    LogoRowIdx logoRowIdx = new LogoRowIdx();
                    logoRowIdx.ID = rowIdx.Attribute("ID")?.Value; // ID 속성을 가져옴

                    var rowDataStr = rowIdx.Value;
                    if (!string.IsNullOrEmpty(rowDataStr))
                    {
                        var rowData = new List<int>();
                        foreach (var token in rowDataStr.Split(','))
                        {
                            if (int.TryParse(token, out var value))
                            {
                                rowData.Add(value);
                            }
                            else
                            {
                                logger.LogWarn($"BoardXmlParser: LogoRowIdx parse failed '{token}' in {xmlFilePath}");
                            }
                        }

                        logoRowIdx.Rows = rowData;
                    }

                    logos.Rows.Add(logoRowIdx);
                }

                boardList.LogoBoard.Add(logos);
            }


            return boardList;
        }
    }
}
