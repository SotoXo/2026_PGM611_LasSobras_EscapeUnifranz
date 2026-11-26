namespace EscapeUNIFRANZ.Map
{
    /// <summary>Relaciona cada zona con su piso: 0 planta baja, 1 piso 2, 2 piso 3.</summary>
    public static class FloorInfo
    {
        public static int FloorOf(string zoneId)
        {
            switch (zoneId)
            {
                case "entrada": case "hall": case "arca": return 0;
                case "piso2": case "aula2": return 1;
                case "piso4_aulas": return 3;
                case "piso5_aulas": return 4;
                default: return 2; // piso3, lab_software, nucleo_ia, aula2
            }
        }

        public static string NameOf(int floor) => floor <= 0 ? "PLANTA BAJA" : "PISO "+(floor+1);
    }
}
