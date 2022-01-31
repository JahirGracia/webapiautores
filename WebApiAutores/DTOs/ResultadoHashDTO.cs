using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiAutores.DTOs
{
    public class ResultadoHashDTO
    {
        public string Hash { get; set; }
        public byte[] Sal { get; set; }
    }
}
