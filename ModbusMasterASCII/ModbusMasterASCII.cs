using ProjectDataLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

namespace nmDriver
{
    public class Driver : IDriverModel, IDisposable
    {
        #region zmienne

        // Constants for access
        private const byte fctReadCoil = 1;

        private const byte fctReadDiscreteInputs = 2;
        private const byte fctReadHoldingRegister = 3;
        private const byte fctReadInputRegister = 4;
        private const byte fctWriteSingleCoil = 5;
        private const byte fctWriteSingleRegister = 6;
        private const byte fctWriteMultipleCoils = 15;
        private const byte fctWriteMultipleRegister = 16;

        //Głowny Buffor danych punkt wejsciowy dla obslugi tagów
        private List<Tag> tagList = new List<Tag>();

        //Beckgroundworker
        [NonSerialized]
        private
        //Watek wtle
        BackgroundWorker bWorker;

        //Informacja o aktywnosci odczytu
        private Boolean isLive;

        //Eksperymntalny delegat
        private delegate byte[] sendAndWait(byte[] dane);

        private delegate Boolean makeTransation(byte[] req);

        #endregion zmienne

        [field: NonSerialized]
        private EventHandler sendInfoEv;

        [field: NonSerialized]
        private EventHandler errorSendEv;

        [field: NonSerialized]
        private EventHandler refreshedPartial;

        [field: NonSerialized]
        private EventHandler refreshCycleEv;

        [field: NonSerialized]
        private EventHandler sendLogInfoEv;

        [field: NonSerialized]
        private EventHandler reciveLogInfoEv;

        /// <summary>
        /// Obszary Pamieci dla sterownika
        /// </summary>
        private static string CoilsName = "Coils";

        private static string DigitalInputsName = "DigitalInputs";
        private static string InputRegisterName = "InputRegisters";
        private static string HoldingRegisterName = "HoldingRegisters";

        private Guid ObjId_;

        Guid IDriverModel.ObjId
        {
            get { return ObjId_; }
        }

        /// <summary>
        /// Zapytanie do urzadzen zdalnych
        /// </summary>
        private List<byte[]> mainFrames = new List<byte[]>();

        /// <summary>
        /// Serialport
        /// </summary>
        private SerialPort sPort;

        /// <summary>
        /// Parametry Transmisji
        /// </summary>
        private IoDriverParam DriverParam_;

        /// <summary>
        /// Nazwa Sterownika
        /// </summary>
        string IDriverModel.driverName
        {
            get { return "ModbusMasterASCII"; }
        }

        /// <summary>
        /// Start Cyklu
        /// </summary>
        /// <param name="tagsList"></param>
        /// <returns></returns>
        bool IDriverModel.activateCycle(List<ITag> tagsList1)
        {
            try
            {
                if (bWorker.IsBusy)
                    return false;

                //Przypisanie danych
                this.tagList.Clear();

                List<Tag> tgs = (from c in tagsList1 where c is Tag select (Tag)c).ToList();

                this.tagList.AddRange(tgs);

                //Konfiguracja danych
                ConfigData(tagList);

                //Nowy client
                sPort = new SerialPort(DriverParam_.PortName, DriverParam_.BoundRate, DriverParam_.parity, DriverParam_.DataBits, DriverParam_.stopBits);
                sPort.Open();

                //Start obiegu
                bWorker.RunWorkerAsync();

                //Comm jest aktywna
                isLive = true;

                foreach (IDriverModel it in this.tagList)
                    it.isAlive = true;

                return true;
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, Ex.Message, Ex));

                if (sPort.IsOpen)
                    sPort.Close();

                return false;
            }
        }

        /// <summary>
        /// Dodanie tagow
        /// </summary>
        /// <param name="tagList"></param>
        /// <returns></returns>
        bool IDriverModel.addTagsComm(List<ITag> tagList1)
        {
            try
            {
                List<Tag> tgs = (from c in tagList1 where c is Tag select (Tag)c).ToList();

                //Dodanie Tagow
                this.tagList.AddRange(tgs);

                //Rekonfiguracja
                ConfigData(this.tagList);

                return true;
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, Ex.Message, Ex));

                return false;
            }
        }

        /// <summary>
        /// Odjecie tagow
        /// </summary>
        /// <param name="tagList"></param>
        /// <returns></returns>
        bool IDriverModel.removeTagsComm(List<ITag> tagList2)
        {
            try
            {
                List<Tag> tgs = (from c in tagList2 where c is Tag select (Tag)c).ToList();

                //usuniecie danych
                foreach (Tag tn in tgs)
                    tagList.Remove(tn);

                //Rekonfiguracja
                ConfigData(this.tagList);

                return true;
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, Ex.Message, Ex));

                return false;
            }
        }

        /// <summary>
        /// Rekonfiguruj dane
        /// </summary>
        /// <returns></returns>
        bool IDriverModel.reConfig()
        {
            try
            {
                ConfigData(this.tagList);

                return true;
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, Ex.Message, Ex));

                return false;
            }
        }

        /// <summary>
        /// Stop Cyklu
        /// </summary>
        /// <returns></returns>
        bool IDriverModel.deactivateCycle()
        {
            isLive = false;

            foreach (IDriverModel it in this.tagList)
                it.isAlive = false;

            return true;
        }

        /// <summary>
        /// Cykl odwierzono
        /// </summary>
        event EventHandler IDriverModel.refreshedCycle
        {
            add { refreshCycleEv += value; }
            remove { refreshCycleEv -= value; }
        }

        /// <summary>
        /// Odswierzenie częsciowe
        /// </summary>
        event EventHandler IDriverModel.refreshedPartial
        {
            add { refreshedPartial += value; }
            remove { refreshedPartial -= value; }
        }

        /// <summary>
        /// Dane wychodzące ze sterownika
        /// </summary>
        event EventHandler IDriverModel.dataSent
        {
            add { sendLogInfoEv += value; }
            remove { sendLogInfoEv -= value; }
        }

        /// <summary>
        /// dane przychodzoce do sterownika
        /// </summary>
        event EventHandler IDriverModel.dataRecived
        {
            add { reciveLogInfoEv += value; }
            remove { reciveLogInfoEv -= value; }
        }

        /// <summary>
        /// Informacja o obszarach pamięci
        /// </summary>
        MemoryAreaInfo[] IDriverModel.MemoryAreaInf
        {
            get
            {
                return new MemoryAreaInfo[] {
                    new MemoryAreaInfo(CoilsName,1,1),
                    new MemoryAreaInfo(DigitalInputsName,1,2),
                    new MemoryAreaInfo(InputRegisterName,16,4),
                    new MemoryAreaInfo(HoldingRegisterName,16,3) };
            }
        }

        /// <summary>
        /// Informacje o blędach
        /// </summary>
        event EventHandler IDriverModel.error
        {
            add { errorSendEv += value; }
            remove { errorSendEv -= value; }
        }

        /// <summary>
        /// Inne informacje z klasy
        /// </summary>
        event EventHandler IDriverModel.information
        {
            add { sendInfoEv += value; }
            remove { sendInfoEv -= value; }
        }

        /// <summary>
        /// Informacja o działąniu komunikacji
        /// </summary>
        bool IDriverModel.isAlive
        {
            get { return isLive; }
            set
            {
                isLive = value;
            }
        }

        /// <summary>
        /// Pluginy dostępne
        /// </summary>
        object[] IDriverModel.plugins
        {
            get { return null; }
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        public Driver()
        {
            try
            {
                //inicjacja bitu
                isLive = false;

                //Parametry Tramsmisji
                DriverParam_ = new IoDriverParam();

                ObjId_ = Guid.NewGuid();

                //Obsluga Backgriundworker
                bWorker = new BackgroundWorker();
                bWorker.WorkerSupportsCancellation = true;
                bWorker.DoWork += new DoWorkEventHandler(bWorker_DoWork);
                bWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bWorker_RunWorkerCompleted);
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterASCII.Constructor :" + Ex.Message, Ex));
            }
        }

        /// <summary>
        /// Głowna praca sterownika - pętla obiegowa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //Sprawdz cy klient jest poloczony
            if (!sPort.IsOpen)
                return;

            //Wysalnie zapytan
            if (mainFrames.Count > 0)
            {
                try
                {
                    //Zapytanie
                    foreach (byte[] fr in mainFrames)
                    {
                        makeTranscation(fr);
                        Thread.Sleep(10);
                    }
                }
                catch (Exception Ex)
                {
                    if (errorSendEv != null)
                        errorSendEv(this, new ProjectEventArgs(mainFrames, DateTime.Now, "ModbusMasterRTU.bWorker_DoWork :" + Ex.Message, Ex));
                }
            }

            #region Zapisz dane na porcie

            //Tagi z obszaru Coils do zapisu
            List<Tag> COWrite = tagList.Where(x => x.areaData.Equals(CoilsName) && x.setMod).ToList();
            if (COWrite.Count > 0)
            {
                try
                {
                    //Próba zapisu Taga
                    foreach (Tag tg in COWrite)
                    {
                        //Utworzenie do wyslania
                        BitArray btArray = new BitArray(tg.coreDataSend);

                        //Utworzenie tablicy byte
                        Byte[] buffByteArr = new byte[tg.coreDataSend.Length / 8 == 0 ? 1 : tg.coreDataSend.Length / 8];

                        //Konwersja i przeniesienie
                        btArray.CopyTo(buffByteArr, 0);

                        //Formowanie ramki
                        byte[] wrCoFrame = CreateWriteHeader((byte)tg.deviceAdress, (ushort)tg.startData, (ushort)tg.coreDataSend.Length, buffByteArr, fctWriteMultipleCoils);
                        tg.setMod = false;

                        //Wyslanie zdarzenia
                        if (sendLogInfoEv != null)
                            sendLogInfoEv(this, new ProjectEventArgs(wrCoFrame, DateTime.Now, getAreaFromFrame(wrCoFrame) + "REQUEST"));

                        //Wyslanie i oczekiwanie na zwrot Danych
                        byte[] rcvCo = writeDataOnPort(wrCoFrame);

                        if (rcvCo != null)
                        {
                            //Wyslanie zdarzenia
                            if (reciveLogInfoEv != null)
                                reciveLogInfoEv(this, new ProjectEventArgs(rcvCo, DateTime.Now, getAreaFromFrame(rcvCo) + "RESPONSE"));

                            //Sprawdzenie
                            if (checkResponse(wrCoFrame, rcvCo))
                            {
                                //Sprawdzenia danych
                            }
                        }
                    }
                }
                catch (Exception Ex)
                {
                    if (errorSendEv != null)
                        errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterRTU.bWorker_DoWork.WriteCO :" + Ex.Message, Ex));
                }
            }

            //Tagi do zapisu w obszarze HoldingRegisters do zapisu
            List<Tag> HRWrite = tagList.Where(x => x.areaData.Equals(HoldingRegisterName) && x.setMod).ToList();
            if (HRWrite.Count > 0)
            {
                try
                {
                    foreach (Tag tg in HRWrite)
                    {
                        //Utworzenie do wyslania
                        BitArray btArray = new BitArray(tg.coreDataSend);
                        //Utworzenie tablicy byte
                        Byte[] buffByteArr = new byte[tg.coreDataSend.Length / 8];
                        //Konwersja i przeniesienie
                        btArray.CopyTo(buffByteArr, 0);

                        //Formowanie ramki
                        byte[] wrHRFrame = CreateWriteHeader((byte)tg.deviceAdress, (ushort)tg.startData, 0, buffByteArr, fctWriteMultipleRegister);
                        tg.setMod = false;

                        //Wyslanie zdarzenia
                        if (sendLogInfoEv != null)
                            sendLogInfoEv(this, new ProjectEventArgs(wrHRFrame, DateTime.Now, getAreaFromFrame(wrHRFrame) + "REQUEST"));

                        //Wyslanie i oczekiwanie na zwrot Danych
                        byte[] rcvBuff = writeDataOnPort(wrHRFrame);

                        //Sprawdzenie
                        if (rcvBuff != null)
                        {
                            //Wyslanie zdarzenia
                            if (reciveLogInfoEv != null)
                                reciveLogInfoEv(this, new ProjectEventArgs(rcvBuff, DateTime.Now, getAreaFromFrame(rcvBuff) + "RESPONSE"));

                            if (checkResponse(wrHRFrame, rcvBuff))
                            {
                                //Puste
                            }
                        }
                    }
                }
                catch (Exception Ex)
                {
                    if (errorSendEv != null)
                        errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterRTU.bWorker_DoWork.WriteHR :" + Ex.Message, Ex));
                }
            }

            #endregion Zapisz dane na porcie

            //Opoznienie wątku o dany czas
            Thread.Sleep(DriverParam_.ReplyTime);
        }

        /// <summary>
        /// Zapisuje dane na porcie
        /// </summary>
        /// <param name="dane"></param>
        /// <returns></returns>
        private byte[] writeDataOnPort(byte[] dane)
        {
            //Zapis na port
            sPort.Write(dane, 0, dane.Length);

            //ilosc danych
            int countData = responseDataCount(dane);

            //Czekanie na odpowiedz
            for (int i = 0; i < DriverParam_.Timeout; i = i + 5)
            {
                //Nadeszły dane
                if (sPort.BytesToRead >= countData)
                {
                    //Buffor na dane
                    byte[] Response = new byte[countData];
                    sPort.Read(Response, 0, Response.Length);
                    return Response;
                }

                //Opoznienie watku
                Thread.Sleep(5);
            }

            //Exception
            if (sPort.BytesToRead == 11)
            {
                byte[] Response = new byte[sPort.BytesToRead];
                sPort.Read(Response, 0, Response.Length);
                return Response;
            }

            //Jezeli program dojedzie do tego miejsca mamy overtime
            return null;
        }

        /// <summary>
        /// Wykonuje Transakcje z urządzniem zdalnym
        /// </summary>
        /// <param name="frRequest"></param>
        /// <returns></returns>
        private Boolean makeTranscation(byte[] frRequest)
        {
            //Zdarzenia
            if (sendLogInfoEv != null)
                sendLogInfoEv(this, new ProjectEventArgs(frRequest, DateTime.Now, getAreaFromFrame(frRequest) + "REQUEST"));

            //Fukcja zwraca dane odpowiedzi jezeli pakiet jest pusty jest Timeout
            byte[] frResponse = writeDataOnPort(frRequest);

            //Sprawdzanie danych
            if (frResponse != null)
            {
                //Wyslanie zdarzenia o nadejsciu danych
                if (reciveLogInfoEv != null)
                    reciveLogInfoEv(this, new ProjectEventArgs(frResponse, DateTime.Now, getAreaFromFrame(frResponse) + "RESPONSE"));

                //Sprawdzenie odpowiedzi
                if (checkResponse(frRequest, frResponse))
                {
                    //Paramery wejsciowe
                    int id = Int32.Parse(String.Format("{0}{1}", (char)frRequest[1], (char)frRequest[2]), NumberStyles.HexNumber);
                    int fct = Int32.Parse(String.Format("{0}{1}", (char)frRequest[3], (char)frRequest[4]), NumberStyles.HexNumber);
                    int startIndex = Int32.Parse(String.Format("{0}{1}{2}{3}", (char)frRequest[5], (char)frRequest[6], (char)frRequest[7], (char)frRequest[8]), NumberStyles.HexNumber);
                    int countData = Int32.Parse(String.Format("{0}{1}{2}{3}", (char)frRequest[9], (char)frRequest[10], (char)frRequest[11], (char)frRequest[12]), NumberStyles.HexNumber);

                    //Zamiana memoryArea ze struktury byte[] na Boolean[]. Odfiltrowanie danych
                    BitArray btArr = new BitArray(getDataFrame(frResponse, countData).ToArray());
                    Boolean[] bitArray = new Boolean[btArr.Length];
                    //Przekopiowanie bitow do tablicy pomocniczej
                    btArr.CopyTo(bitArray, 0);

                    //Obszar pamiecie
                    MemoryAreaInfo mInfo = ((IDriverModel)this).MemoryAreaInf.Where(x => x.fctCode == fct).ToArray()[0];

                    //Selekcja tagow do przeslania do odswierzenia
                    var zm = from n in tagList
                             where n.deviceAdress == id && n.areaData == mInfo.Name && n.bitAdres >= startIndex * mInfo.AdresSize && n.bitAdres + n.coreData.Length <= (startIndex + countData) * mInfo.AdresSize
                             select n;

                    //Lista
                    List<Tag> refDataTag = zm.ToList();

                    //Przypisane danych do tagow
                    for (int i = 0; i < refDataTag.Count; i++)
                    {
                        Array.Copy(bitArray, refDataTag[i].bitAdres - startIndex * mInfo.AdresSize, refDataTag[i].coreData, 0, refDataTag[i].coreData.Length);
                        refDataTag[i].refreshData();
                    }

                    //Odswierzenie danych informacja. Delegat do pozostałych klass
                    if (refreshCycleEv != null)
                        refreshCycleEv(this, new ProjectEventArgs(refDataTag));

                    return true;
                }
            }
            else
            {
                //OverTime
                if (sendInfoEv != null)
                    sendInfoEv(this, new ProjectEventArgs(frRequest, DateTime.Now, "Device not respond. " + getAreaFromFrame(frRequest) + "- Timeout"));

                return false;
            }

            return false;
        }

        /// <summary>
        /// Pobranie dane z ramki odpowiedzi
        /// </summary>
        /// <param name="dane"></param>
        /// <returns></returns>
        private List<byte> getDataFrame(byte[] dane, int count)
        {
            try
            {
                //Count Data
                int countData = Int32.Parse(String.Format("{0}{1}", (char)dane[5], (char)dane[6]), NumberStyles.HexNumber);

                //Pobranie nagłowka
                Byte[] header = dane.ToList().GetRange(7, countData * 2).ToArray();

                //Lista buforowa
                List<byte> lista = new List<byte>();

                //Wyslanie ramki
                for (int i = 0; i < header.Length; i = i + 2)
                    lista.Add(Byte.Parse(String.Format("{0}{1}", (char)header[i], (char)header[i + 1]), NumberStyles.HexNumber));

                return lista;
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterASCII.getDataFrame :" + Ex.Message, Ex));

                return null;
            }
        }

        /// <summary>
        /// Praca wykonana i przejscie do nastpnej albo zatrzymanie
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                //Reaktywacja pobierania danych
                if (isLive)
                {
                    bWorker.RunWorkerAsync();
                }
                else
                {
                    sPort.Close();
                }
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterASCII.bWorker_RunWorkerCompleted :" + Ex.Message, Ex));

                if (sPort.IsOpen)
                    sPort.Close();
            }
        }

        /// <summary>
        /// Konfiguracja parmamertów Bufforów dla rejestrow
        /// </summary>
        /// <param name="tagList"></param>
        private void ConfigData(List<Tag> tagList)
        {
            //Testowanie strownika
            if (tagList == null)
                return;

            //Tablica zapytań
            List<byte[]> buffFrames = new List<byte[]>();

            //Pobranie adresów
            List<ushort> adreses = tagList.Select(x => x.deviceAdress).ToList();

            //Pobranie unikalnych adresow
            adreses = adreses.Distinct().ToList();

            //Tworzenie Ramek
            foreach (int i in adreses)
                buffFrames.AddRange(configSingleDevice(tagList.Where(x => x.deviceAdress == i).ToList(), i));

            //Przypisanie zapytań
            mainFrames = buffFrames;
        }

        /// <summary>
        /// Program zakresy odpytan dla jednego adresu
        /// </summary>
        /// <param name="tgs"></param>
        /// <returns></returns>
        private List<byte[]> configSingleDevice(List<Tag> tgs, int adress)
        {
            int maxFrameSizeCO = 230 * 8;
            int maxSizeCO = 130;

            int maxFrameSizeDI = 230 * 8;
            int maxSizeDI = 130;

            int maxFrameSizeHR = 110;
            int maxSizeHR = 20;

            int maxFrameSizeIR = 110;
            int maxSizeIR = 20;

            //Tablica pomocnicza
            List<byte[]> buffFrames = new List<byte[]>();

            try
            {
                //Znalezienie obszarów max i min do odczytu dla całego odwzorowania rejestrów
                //Szakuanie granic odswierzania COILS
                List<Tag> coilsTag = new List<Tag>();
                coilsTag = tgs.Where(x => x.areaData.Equals(CoilsName)).ToList<Tag>();
                if (coilsTag.Count > 0)
                {
                    //Najnizszy i najwyzszy adres tagowy.
                    int min = coilsTag.Min(x => x.startData);
                    int max = coilsTag.Max(x => (x.startData + x.coreData.Length)) - 1;
                    int marker = 0;

                    //Lista punktow do utworzenia ramek
                    List<PO> points = new List<PO>();
                    points.Add(new PO(min, 0));

                    //Petla obiegowa
                    for (int i = min; i <= max; i++)
                    {
                        //Zabezpiecznie dlugiej ramki
                        if (points[points.Count - 1].Y - points[points.Count - 1].X >= maxFrameSizeCO)
                        {
                            //Jestemy w miejscu Taga trzeba podac koniec
                            Tag halfTag = coilsTag.Where(x => x.startData <= i && (x.startData + x.coreData.Length) > i).ToList()[0];
                            if (halfTag != null)
                            {
                                points[points.Count - 1].Y = halfTag.startData + halfTag.coreData.Length - 1;
                                i = points[points.Count - 1].Y + 1;
                                points.Add(new PO(i, 0));
                            }
                            else
                                points.Add(new PO(i, 0));
                        }

                        //Zabezpieczenie przed przekroczeniem wolnego odstepu
                        if (marker == maxSizeCO)
                            points.Add(new PO(i, 0));

                        //Sprawdzenie czy tagi sa w zakresie
                        if (coilsTag.Where(x => x.startData <= i && (x.startData + x.coreData.Length) > i).ToList().Count > 0)
                        {
                            //DANIE
                            points[points.Count - 1].Y = i;

                            //Dociagnicie do adresu po powrocie do odliczania
                            if (marker >= maxSizeCO)
                                points[points.Count - 1].X = i;

                            marker = 0;
                        }
                        else
                        {
                            //Przesuwaj X
                            if (marker >= maxSizeCO)
                                points[points.Count - 1].X = i;

                            //Inkrementuj marker
                            marker++;
                        }
                    }

                    //Utwzorzenie ramek
                    foreach (PO p in points)
                    {
                        buffFrames.Add(CreateReadHeader((byte)adress, (ushort)p.X, (ushort)((p.Y - p.X) + 1), fctReadCoil));
                    }
                }
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterRTU.configSingleDevice.CO :" + Ex.Message, Ex));
            }

            try
            {
                //Znalezienie obszarów max i min do odczytu dla całego odwzorowania rejestrów
                //Szakuanie granic odswierzania DISCRETEINPUT
                List<Tag> DiscreteInputTag = new List<Tag>();
                DiscreteInputTag = tgs.Where(x => x.areaData.Equals(DigitalInputsName)).ToList<Tag>();
                if (DiscreteInputTag.Count > 0)
                {
                    //Najnizszy i najwyzszy adres tagowy.
                    int min = DiscreteInputTag.Min(x => x.startData);
                    int max = DiscreteInputTag.Max(x => (x.startData + x.coreData.Length)) - 1;
                    int marker = 0;

                    //Lista punktow do utworzenia ramek
                    List<PO> points = new List<PO>();
                    points.Add(new PO(min, 0));

                    //Petla obiegowa
                    for (int i = min; i <= max; i++)
                    {
                        //Zabezpiecznie dlugiej ramki
                        if (points[points.Count - 1].Y - points[points.Count - 1].X >= maxFrameSizeDI)
                        {
                            //Jestemy w miejscu Taga trzeba podac koniec
                            Tag halfTag = DiscreteInputTag.Where(x => x.startData <= i && (x.startData + x.coreData.Length) > i).ToList()[0];
                            if (halfTag != null)
                            {
                                points[points.Count - 1].Y = halfTag.startData + halfTag.coreData.Length - 1;
                                i = points[points.Count - 1].Y + 1;
                                points.Add(new PO(i, 0));
                            }
                            else
                                points.Add(new PO(i, 0));
                        }

                        //Zabezpieczenie przed przekroczeniem wolnego odstepu
                        if (marker == maxSizeDI)
                            points.Add(new PO(i, 0));

                        //Sprawdzenie czy tagi sa w zakresie
                        if (DiscreteInputTag.Where(x => x.startData <= i && (x.startData + x.coreData.Length) > i).ToList().Count > 0)
                        {
                            points[points.Count - 1].Y = i;

                            //Dociagnicie do adresu po powrocie do odliczania
                            if (marker >= maxSizeDI)
                                points[points.Count - 1].X = i;

                            marker = 0;
                        }
                        else
                        {
                            //Przesuwaj X
                            if (marker >= maxSizeDI)
                                points[points.Count - 1].X = i;

                            //Inkrementuj marker
                            marker++;
                        }
                    }

                    //Utwzorzenie ramek
                    foreach (PO p in points)
                    {
                        buffFrames.Add(CreateReadHeader((byte)adress, (ushort)p.X, (ushort)((p.Y - p.X) + 1), fctReadDiscreteInputs));
                    }
                }
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterRTU.configSingleDevice.DI :" + Ex.Message, Ex));
            }

            try
            {
                //Znalezienie obszarów max i min do odczytu dla całego odwzorowania rejestrów
                List<Tag> InputRegisterTag = new List<Tag>();
                InputRegisterTag = tgs.Where(x => x.areaData.Equals(InputRegisterName)).ToList<Tag>();
                if (InputRegisterTag.Count > 0)
                {
                    //Najnizszy i najwyzszy adres tagowy.
                    int min = InputRegisterTag.Min(x => x.startData);
                    int max = InputRegisterTag.Max(x => (x.startData + x.coreData.Length / 16)) - 1;
                    int marker = 0;

                    //Lista punktow do utworzenia ramek
                    List<PO> points = new List<PO>();
                    points.Add(new PO(min, 0));

                    //Petla obiegowa
                    for (int i = min; i <= max; i++)
                    {
                        //Zabezpiecznie dlugiej ramki
                        if (points[points.Count - 1].Y - points[points.Count - 1].X >= maxFrameSizeIR)
                        {
                            Tag halfTag = InputRegisterTag.Where(x => x.startData <= i && (x.startData + x.coreData.Length / 16) > i).ToList()[0];
                            if (halfTag != null)
                            {
                                points[points.Count - 1].Y = halfTag.startData + (halfTag.coreData.Length / 16) - 1;
                                i = points[points.Count - 1].Y + 1;
                                points.Add(new PO(i, 0));
                            }
                            else
                                points.Add(new PO(i, 0));
                        }

                        //Zabezpieczenie przed przekroczeniem wolnego odstepu
                        if (marker == maxSizeIR)
                            points.Add(new PO(i, 0));

                        //Sprawdzenie czy tagi sa w zakresie
                        if (InputRegisterTag.Where(x => x.startData <= i && (x.startData + x.coreData.Length / 16) > i).ToList().Count > 0)
                        {
                            points[points.Count - 1].Y = i;

                            //Dociagnicie do adresu po powrocie do odliczania
                            if (marker >= maxSizeIR)
                                points[points.Count - 1].X = i;

                            marker = 0;
                        }
                        else
                        {
                            //Przesuwaj X
                            if (marker >= maxSizeIR)
                                points[points.Count - 1].X = i;

                            //Inkrementuj marker
                            marker++;
                        }
                    }

                    //Utwzorzenie ramek
                    foreach (PO p in points)
                    {
                        buffFrames.Add(CreateReadHeader((byte)adress, (ushort)p.X, (ushort)((p.Y - p.X) + 1), fctReadInputRegister));
                    }
                }
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterRTU.configSingleDevice.IR :" + Ex.Message, Ex));
            }

            try
            {
                //Znalezienie obszarów max i min do odczytu dla całego odwzorowania rejestrów
                List<Tag> HoldingRegisterTag = new List<Tag>();
                HoldingRegisterTag = tgs.Where(x => x.areaData.Equals(HoldingRegisterName)).ToList<Tag>();
                if (HoldingRegisterTag.Count > 0)
                {
                    //Najnizszy i najwyzszy adres tagowy.
                    int min = HoldingRegisterTag.Min(x => x.startData);
                    int max = HoldingRegisterTag.Max(x => (x.startData + x.coreData.Length / 16)) - 1;
                    int marker = 0;

                    //Lista punktow do utworzenia ramek
                    List<PO> points = new List<PO>();
                    points.Add(new PO(min, 0));

                    //Petla obiegowa
                    for (int i = min; i <= max; i++)
                    {
                        //Zabezpiecznie dlugiej ramki
                        if (points[points.Count - 1].Y - points[points.Count - 1].X >= maxFrameSizeHR)
                        {
                            //Jestemy w miejscu Taga trzeba podac koniec
                            Tag halfTag = HoldingRegisterTag.Where(x => x.startData <= i && (x.startData + x.coreData.Length / 16) > i).ToList()[0];
                            if (halfTag != null)
                            {
                                points[points.Count - 1].Y = halfTag.startData + (halfTag.coreData.Length / 16) - 1;
                                i = points[points.Count - 1].Y + 1;
                                points.Add(new PO(i, 0));
                            }
                            else
                                points.Add(new PO(i, 0));
                        }

                        //Zabezpieczenie przed przekroczeniem wolnego odstepu
                        if (marker == maxSizeHR)
                            points.Add(new PO(i, 0));

                        //Sprawdzenie czy tagi sa w zakresie
                        if (HoldingRegisterTag.Where(x => x.startData <= i && (x.startData + x.coreData.Length / 16) > i).ToList().Count > 0)
                        {
                            points[points.Count - 1].Y = i;

                            //Dociagnicie do adresu po powrocie do odliczania
                            if (marker >= maxSizeHR)
                                points[points.Count - 1].X = i;

                            marker = 0;
                        }
                        else
                        {
                            //Przesuwaj X
                            if (marker >= maxSizeHR)
                                points[points.Count - 1].X = i;

                            //Inkrementuj marker
                            marker++;
                        }
                    }

                    //Utwzorzenie ramek
                    foreach (PO p in points)
                    {
                        buffFrames.Add(CreateReadHeader((byte)adress, (ushort)p.X, (ushort)((p.Y - p.X) + 1), fctReadHoldingRegister));
                    }
                }
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterRTU.configSingleDevice.HR :" + Ex.Message, Ex));
            }

            return buffFrames;
        }

        /// <summary>
        /// Stworzenie nagłówka dla odczytu
        /// </summary>
        /// <param name="id"></param>
        /// <param name="startAddress"></param>
        /// <param name="length"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        private byte[] CreateReadHeader(byte id, ushort startAddress, ushort length, byte function)
        {
            List<byte> data = new List<byte>();
            List<byte> lrc = new List<byte>();

            //Prefiks
            data.Add((byte)':');

            //Id
            data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", id)));
            lrc.Add(id);

            //Function
            data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", function)));
            lrc.Add(function);

            //Start adress
            data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X4}", startAddress)));
            lrc.AddRange(BitConverter.GetBytes(startAddress));

            //Count Data
            data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X4}", length)));
            lrc.AddRange(BitConverter.GetBytes(length));

            //LRC
            byte lrcCal = calculateLRC(lrc.ToArray());
            data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", lrcCal)));

            //CR
            data.Add(13);

            //LF
            data.Add(10);

            //Przeslanie danych
            return data.ToArray();
        }

        /// <summary>
        /// Stworzenie nagłowka dla zapisu
        /// </summary>
        /// <param name="id"></param>
        /// <param name="startAddress"></param>
        /// <param name="countData"></param>
        /// <param name="value"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        private byte[] CreateWriteHeader(byte id, ushort startAddress, ushort countData, byte[] value, byte function)
        {
            if (function == fctWriteSingleCoil)
            {
                List<byte> data = new List<byte>();
                List<byte> lrc = new List<byte>();

                //Prefiks
                data.Add((byte)':');

                //Id
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", id)));
                lrc.Add(id);

                //function
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", function)));
                lrc.Add(function);

                //Start adress
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X4}", startAddress)));
                lrc.AddRange(BitConverter.GetBytes(startAddress));

                //Data
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}{1:X2}", value[0], 0)));
                lrc.Add(value[0]);

                //LRC
                byte lrcCal = calculateLRC(lrc.ToArray());
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", lrcCal)));

                //CR
                data.Add(13);

                //LF
                data.Add(10);

                //Przeslanie danych
                return data.ToArray();
            }

            if (function == fctWriteMultipleCoils)
            {
                List<byte> data = new List<byte>();
                List<byte> lrc = new List<byte>();

                //Prefiks
                data.Add((byte)':');

                //Id
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", id)));
                lrc.Add(id);

                //function
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", function)));
                lrc.Add(function);

                //Start adress
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X4}", startAddress)));
                lrc.AddRange(BitConverter.GetBytes(startAddress));

                //Ilość danych
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X4}", (byte)(value.Length * 8))));
                lrc.Add((byte)(value.Length * 8));

                //Ilość nestepnych bajtow
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", (byte)(value.Length))));
                lrc.Add((byte)(value.Length));

                //Dane do zapisu
                for (int i = 0; i < value.Length; i++)
                {
                    data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", (byte)(value[i]))));
                    lrc.Add(value[i]);
                }

                //LRC
                byte lrcCal = calculateLRC(lrc.ToArray());
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", lrcCal)));

                //CR
                data.Add(13);

                //LF
                data.Add(10);

                //Przeslanie danych
                return data.ToArray();
            }

            if (function == fctWriteSingleRegister)
            {
                List<byte> data = new List<byte>();
                List<byte> lrc = new List<byte>();

                //Prefiks
                data.Add((byte)':');

                //Id
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", id)));
                lrc.Add(id);

                //function
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", function)));
                lrc.Add(function);

                //Start adress
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X4}", startAddress)));
                lrc.AddRange(BitConverter.GetBytes(startAddress));

                //Data1
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", value[0])));
                lrc.Add(value[0]);

                //Data2
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", value[1])));
                lrc.Add(value[1]);

                //LRC
                byte lrcCal = calculateLRC(lrc.ToArray());
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", lrcCal)));

                //CR
                data.Add(13);

                //LF
                data.Add(10);

                //Przeslanie danych
                return data.ToArray();
            }

            if (function == fctWriteMultipleRegister)
            {
                List<byte> data = new List<byte>();
                List<byte> lrc = new List<byte>();

                //Prefiks
                data.Add((byte)':');

                //Id
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", id)));
                lrc.Add(id);

                //function
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", function)));
                lrc.Add(function);

                //Start adress
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X4}", startAddress)));
                lrc.AddRange(BitConverter.GetBytes(startAddress));

                //Ilość rejestrow
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X4}", (byte)(value.Length / 2))));
                lrc.Add((byte)(value.Length / 2));

                //Ilość nestepnych bajtow
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", (byte)(value.Length))));
                lrc.Add((byte)(value.Length));

                //Dane do zapisu
                for (int i = 0; i < value.Length; i++)
                {
                    data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", (byte)(value[i]))));
                    lrc.Add(value[i]);
                }

                //LRC
                byte lrcCal = calculateLRC(lrc.ToArray());
                data.AddRange(ASCIIEncoding.Default.GetBytes(String.Format("{0:X2}", lrcCal)));

                //CR
                data.Add(13);

                //LF
                data.Add(10);

                //Przeslanie danych
                return data.ToArray();
            }
            else
                return null;
        }

        /// <summary>
        /// Sparwdzenie odpowiedzi
        /// </summary>
        /// <param name="zapytanie"></param>
        /// <param name="odpowiedz"></param>
        /// <returns></returns>
        private Boolean checkResponse(byte[] zapytanie, byte[] odpowiedz)
        {
            try
            {
                //LRC Conversion
                var table = new List<byte>();
                for (int i = 1; i < odpowiedz.Length - 4; i = i + 2)
                    table.Add(byte.Parse(String.Format("{0}{1}", (char)odpowiedz[i], (char)odpowiedz[i + 1]), NumberStyles.HexNumber));

                //LRC
                byte lrc_result = calculateLRC(table.ToArray());
                byte lrcResponse = Byte.Parse(String.Format("{0}{1}", (char)odpowiedz[odpowiedz.Length - 4], (char)odpowiedz[odpowiedz.Length - 3]), NumberStyles.HexNumber);

                if (lrc_result != lrcResponse)
                    throw new ApplicationException(String.Format("CRC problem for function: {0}{1}", (char)odpowiedz[1], (char)odpowiedz[2]));

                //Normalna odpowiedz
                if (odpowiedz.Length > 11)
                {
                    //Paramery wejsciowe
                    int id = Int32.Parse(String.Format("{0}{1}", (char)zapytanie[1], (char)zapytanie[2]), NumberStyles.HexNumber);
                    int fct = Int32.Parse(String.Format("{0}{1}", (char)zapytanie[3], (char)zapytanie[4]), NumberStyles.HexNumber);
                    int startIndex = Int32.Parse(String.Format("{0}{1}{2}{3}", (char)zapytanie[5], (char)zapytanie[6], (char)zapytanie[7], (char)zapytanie[8]), NumberStyles.HexNumber);
                    int countData = Int32.Parse(String.Format("{0}{1}{2}{3}", (char)zapytanie[9], (char)zapytanie[10], (char)zapytanie[11], (char)zapytanie[12]), NumberStyles.HexNumber);

                    int id_R = Int32.Parse(String.Format("{0}{1}", (char)odpowiedz[1], (char)odpowiedz[2]), NumberStyles.HexNumber);
                    int fct_R = Int32.Parse(String.Format("{0}{1}", (char)odpowiedz[3], (char)odpowiedz[4]), NumberStyles.HexNumber);

                    //Testujemy ID
                    if (id_R != id)
                        throw new ApplicationException("ID - response isnt correct.");

                    //Testujemy FCT
                    if (fct_R != fct)
                        throw new ApplicationException("Response code function isnt correct.");

                    #region WriteSingleCoil and WriteSingleRegester

                    if (fct == 5 || fct == 6)
                    {
                        if (!Enumerable.SequenceEqual(zapytanie.ToList().GetRange(0, 13), odpowiedz.ToList().GetRange(0, 13)))
                            throw new ApplicationException("Response and Request isnt equals - (fct 5, 6).");
                    }

                    #endregion WriteSingleCoil and WriteSingleRegester

                    #region WriteMultipleCoil and WriteMultipleRegester

                    if (zapytanie[1] == 15 || zapytanie[1] == 16)
                    {
                        if (!Enumerable.SequenceEqual(zapytanie.ToList().GetRange(0, 6), odpowiedz.ToList().GetRange(0, 6)))
                            throw new ApplicationException("Response and Request isn't equals - (fct 15, 16).");
                    }

                    #endregion WriteMultipleCoil and WriteMultipleRegester
                }

                //Prawdopodobnie exeption
                else if (odpowiedz.Length == 11)
                {
                    //Jezeli CRC jest ok rzuć wyjatek
                    throw new ApplicationException(String.Format("Exception - Function: {0}    Code: {1}", String.Format("{0}{1}", (char)odpowiedz[3], (char)odpowiedz[4]), String.Format("{0}{1}", (char)odpowiedz[5], (char)odpowiedz[6])));
                }
                else
                    //Niepoprawna dlugość ramki
                    throw new ApplicationException("Bad format frame, data length to low.");

                return true;
            }
            catch (Exception Ex)
            {
                if (sendInfoEv != null)
                    sendInfoEv(this, new ProjectEventArgs(odpowiedz, DateTime.Now, "ModbusMasterASCII.checkResponse :" + Ex.Message));

                return false;
            }
        }

        /// <summary>
        /// Pobiera nazwe obszaru pamieci na podstawie ramki
        /// </summary>
        /// <param name="ask"></param>
        /// <returns></returns>
        private string getAreaFromFrame(byte[] ask)
        {
            //Kod fukcji
            int fct = Int32.Parse(String.Format("{0}{1}", (char)ask[3], (char)ask[4]), NumberStyles.HexNumber);

            switch (fct)
            {
                case 1:
                    return CoilsName.ToUpper() + ".READ.";

                case 2:
                    return DigitalInputsName.ToUpper() + ".READ.";

                case 3:
                    return HoldingRegisterName.ToUpper() + ".READ.";

                case 4:
                    return InputRegisterName.ToUpper() + ".READ.";

                case 5:
                    return CoilsName.ToUpper() + ".WRITE.";

                case 6:
                    return HoldingRegisterName.ToUpper() + ".WRITE.";

                case 15:
                    return CoilsName.ToUpper() + ".WRITE.";

                case 16:
                    return HoldingRegisterName.ToUpper() + ".WRITE.";

                case 129:
                    return " Exception Nr: 129 (HEX: 81) ";

                case 130:
                    return " Exception Nr: 130 (HEX: 82) ";

                case 131:
                    return " Exception Nr: 131 (HEX: 83) ";

                case 132:
                    return " Exception Nr: 132 (HEX: 84) ";

                case 133:
                    return " Exception Nr: 133 (HEX: 85) ";

                case 134:
                    return " Exception Nr: 134 (HEX: 86) ";

                case 143:
                    return " Exception Nr: 143 (HEX: 8F) ";

                case 144:
                    return " Exception Nr: 144 (HEX: 90) ";

                default:
                    return ".NO NAME.";
            }
        }

        /// <summary>
        /// Obliczenie ile danych jest potrzebnych
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        private int responseDataCount(byte[] frame)
        {
            int id = Int32.Parse(String.Format("{0}{1}", (char)frame[1], (char)frame[2]), NumberStyles.HexNumber);
            int fct = Int32.Parse(String.Format("{0}{1}", (char)frame[3], (char)frame[4]), NumberStyles.HexNumber);
            int start = Int32.Parse(String.Format("{0}{1}{2}{3}", (char)frame[5], (char)frame[6], (char)frame[7], (char)frame[8]), NumberStyles.HexNumber);
            int countReq = Int32.Parse(String.Format("{0}{1}{2}{3}", (char)frame[9], (char)frame[10], (char)frame[11], (char)frame[12]), NumberStyles.HexNumber);

            #region Read CO i DI

            //Read DI and CO
            if (fct == 1 || fct == 2)
            {
                //DI CO
                int reszta = (int)(float)(countReq % 8);
                int byteCount = countReq / 8;

                //Sprawdzenie czy jest reszta
                if (reszta > 0)
                    byteCount++;

                return (byteCount * 2) + 11;
            }

            #endregion Read CO i DI

            #region Read HR i DI

            if (fct == 3 || fct == 4)
            {
                //kalkulcja odpowidzi
                return 11 + (countReq * 4);
            }

            #endregion Read HR i DI

            #region Write sDI i sHR

            if (fct == 5 || fct == 6)
                return 17;

            #endregion Write sDI i sHR

            #region Write mDI i mHR

            if (fct == 15 || fct == 16)
                return 17;

            #endregion Write mDI i mHR

            return 500;
        }

        /// <summary>
        /// Ustawienie klasy parametryzacji transmisji
        /// </summary>
        object IDriverModel.setDriverParam
        {
            get
            {
                return DriverParam_;
            }
            set
            {
                DriverParam_ = (IoDriverParam)value;
            }
        }

        /// <summary>
        /// Formatowanie ramki w zalezności od odpowiedzi
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        string IDriverModel.FormatFrameRequest(byte[] frame, NumberStyles num)
        {
            try
            {
                String modbusHeader = String.Empty;
                String restData = string.Empty;

                //Paramery wejsciowe
                int id = Int32.Parse(String.Format("{0}{1}", (char)frame[1], (char)frame[2]), NumberStyles.HexNumber);
                int fct = Int32.Parse(String.Format("{0}{1}", (char)frame[3], (char)frame[4]), NumberStyles.HexNumber);
                int startIndex = Int32.Parse(String.Format("{0}{1}{2}{3}", (char)frame[5], (char)frame[6], (char)frame[7], (char)frame[8]), NumberStyles.HexNumber);
                int countData = Int32.Parse(String.Format("{0}{1}{2}{3}", (char)frame[9], (char)frame[10], (char)frame[11], (char)frame[12]), NumberStyles.HexNumber);

                switch (num)
                {
                    case NumberStyles.HexNumber:
                        if (fct <= 4)
                        {
                            modbusHeader = String.Format("{0:X}{1:X}{2:X}   {3:X}{4:X}   {5:X}{6:X}{7:X}{8:X}   {9:X}{10:X}{11:X}{12:X}   {13:X}{14:X}",
                                           frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7], frame[8], frame[9], frame[10], frame[11], frame[12], frame[13], frame[14]);
                            return modbusHeader;
                        }

                        if (fct == 5 || fct == 6)
                        {
                            modbusHeader = String.Format("{0:X}{1:X}{2:X}   {3:X}{4:X}   {5:X}{6:X}{7:X}{8:X}   {9:X}{10:X}{11:X}{12:X}   {13:X}{14:X}",
                                           frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7], frame[8], frame[9], frame[10], frame[11], frame[12], frame[13], frame[14]);
                            return modbusHeader;
                        }
                        else
                        {
                            modbusHeader = String.Format("{0:X}{1:X}{2:X}   {3:X}{4:X}   {5:X}{6:X}{7:X}{8:X}   {9:X}{10:X}{11:X}{12:X}   {13:X}{14:X}",
                                           frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7], frame[8], frame[9], frame[10], frame[11], frame[12], frame[13], frame[14]);

                            return modbusHeader;
                        }

                    case NumberStyles.Integer:
                        if (fct <= 4)
                        {
                            modbusHeader = String.Format("{0}{1}{2}   {3}{4}   {5}{6}{7}{8}   {9}{10}{11}{12}   {13}{14}",
                                           frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7], frame[8], frame[9], frame[10], frame[11], frame[12], frame[13], frame[14]);
                            return modbusHeader;
                        }

                        if (fct == 5 || fct == 6)
                        {
                            modbusHeader = String.Format("{0}{1}{2}   {3}{4}   {5}{6}{7}{8}   {9}{10}{11}{12}   {13}{14}",
                                           frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7], frame[8], frame[9], frame[10], frame[11], frame[12], frame[13], frame[14]);
                            return modbusHeader;
                        }
                        else
                        {
                            modbusHeader = String.Format("{0}{1}{2}   {3}{4}   {5}{6}{7}{8}   {9}{10}{11}{12}   {13}{14}",
                                           frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7], frame[8], frame[9], frame[10], frame[11], frame[12], frame[13], frame[14]);

                            return modbusHeader;
                        }

                    default:
                        if (fct <= 4)
                        {
                            modbusHeader = String.Format("{0}{1}{2}   {3}{4}   {5}{6}{7}{8}   {9}{10}{11}{12}   {13}{14}",
                                          (char)frame[0], (char)frame[1], (char)frame[2], (char)frame[3], (char)frame[4], (char)frame[5], (char)frame[6], (char)frame[7],
                                          (char)frame[8], (char)frame[9], (char)frame[10], (char)frame[11], (char)frame[12], (char)frame[13], (char)frame[14]);
                            return modbusHeader;
                        }

                        if (fct == 5 || fct == 6)
                        {
                            modbusHeader = String.Format("{0}{1}{2}   {3}{4}   {5}{6}{7}{8}   {9}{10}{11}{12}   {13}{14}",
                                           (char)frame[0], (char)frame[1], (char)frame[2], (char)frame[3], (char)frame[4], (char)frame[5], (char)frame[6], (char)frame[7],
                                           (char)frame[8], (char)frame[9], (char)frame[10], (char)frame[11], (char)frame[12], (char)frame[13], (char)frame[14]);
                            return modbusHeader;
                        }
                        else
                        {
                            modbusHeader = String.Format("{0}{1}{2}   {3}{4}   {5}{6}{7}{8}   {9}{10}{11}{12}   {13}{14}",
                                           (char)frame[0], (char)frame[1], (char)frame[2], (char)frame[3], (char)frame[4], (char)frame[5], (char)frame[6], (char)frame[7],
                                           (char)frame[8], (char)frame[9], (char)frame[10], (char)frame[11], (char)frame[12], (char)frame[13], (char)frame[14]);

                            return modbusHeader;
                        }
                }
            }
            catch (Exception Ex)
            {
                if (sendInfoEv != null)
                    sendInfoEv(this, new ProjectEventArgs(frame, DateTime.Now, "ModbusMasterASCII.FormatFrameRequest :" + Ex.Message));

                return "Fault: " + frame.Aggregate("", (sum, next) => sum + " " + next.ToString());
            }
        }

        /// <summary>
        /// Formatowanie ramki przychodzącej
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        string IDriverModel.FormatFrameResponse(byte[] frame, NumberStyles num)
        {
            try
            {
                String modbusHeader = String.Empty;
                String restData = String.Empty;

                //Paramery wejsciowe
                int id = Int32.Parse(String.Format("{0}{1}", (char)frame[1], (char)frame[2]), NumberStyles.HexNumber);
                int fct = Int32.Parse(String.Format("{0}{1}", (char)frame[3], (char)frame[4]), NumberStyles.HexNumber);
                int countData = Int32.Parse(String.Format("{0}{1}", (char)frame[5], (char)frame[6]), NumberStyles.HexNumber);

                switch (num)
                {
                    case NumberStyles.HexNumber:
                        //Read Area
                        if (fct <= 4)
                        {
                            modbusHeader = String.Format("{0:X}{1:X}{2:X}   {3:X}{4:X}", frame[0], frame[1], frame[2], frame[3], frame[4]);
                            restData = frame.ToList().GetRange(7, countData * 2).Aggregate(String.Format("{0:X}{1:X}   ", frame[5], frame[6]), (sum, next) => sum + " " + String.Format("{0:X}", next));
                            return modbusHeader + "   " + restData + String.Format("   {0:X}{1:X}", frame[frame.Length - 4], frame[frame.Length - 3]); ;
                        }
                        //Single Cols and Single Register
                        else if (fct == 5 || fct == 6 || fct == 15 || fct == 16)
                        {
                            modbusHeader = String.Format("{0:X}{1:X}{2:X}   {3:X}{4:X}   {5:X}{6:X}{7:X}{8:X}   {9:X}{10:X}{11:X}{12:X}   {13:X}{14:X}",
                                           frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7], frame[8], frame[9], frame[10], frame[11], frame[12], frame[13], frame[14]);
                            return modbusHeader;
                        }
                        //Exceptions
                        else
                        {
                            modbusHeader = String.Format("{0:X}{1:X}{2:X}   {3:X}{4:X}   {5:X}{6:X}   {7:X}{8:X}",
                                frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7], frame[8]);
                            return modbusHeader;
                        }

                    case NumberStyles.Integer:
                        //Read Area
                        if (fct <= 4)
                        {
                            modbusHeader = String.Format("{0}{1}{2}    {3}{4}", frame[0], frame[1], frame[2], frame[3], frame[4]);
                            restData = frame.ToList().GetRange(7, countData * 2).Aggregate(String.Format("{0}{1}   ", frame[5], frame[6]), (sum, next) => sum + " " + String.Format("{0}", next));
                            return modbusHeader + "   " + restData + String.Format("   {0}{1}", frame[frame.Length - 4], frame[frame.Length - 3]); ;
                        }
                        //Single Cols and Single Register
                        else if (fct == 5 || fct == 6 || fct == 15 || fct == 16)
                        {
                            modbusHeader = String.Format("{0}{1}{2}   {3}{4}   {5}{6}{7}{8}   {9}{10}{11}{12}   {13}{14}",
                                           frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7], frame[8], frame[9], frame[10], frame[11], frame[12], frame[13], frame[14]);
                            return modbusHeader;
                        }
                        //Exceptions
                        else
                        {
                            modbusHeader = String.Format("{0}{1}{2}   {3}{4}   {5}{6}   {7}{8}",
                                frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7], frame[8]);
                            return modbusHeader;
                        }

                    default:
                        //Read Area
                        if (fct <= 4)
                        {
                            modbusHeader = String.Format("{0}{1}{2}   {3}{4}", (char)frame[0], (char)frame[1], (char)frame[2], (char)frame[3], (char)frame[4]);
                            restData = frame.ToList().GetRange(7, countData * 2).Aggregate(String.Format("{0}{1}   ", (char)frame[5], (char)frame[6]), (sum, next) => sum + String.Format("{0}", (char)next));
                            return modbusHeader + "   " + restData + String.Format("   {0}{1}", (char)frame[frame.Length - 4], (char)frame[frame.Length - 3]);
                        }
                        //Single Cols and Single Register
                        else if (fct == 5 || fct == 6 || fct == 15 || fct == 16)
                        {
                            modbusHeader = String.Format("{0}{1}{2}   {3}{4}   {5}{6}{7}{8}   {9}{10}{11}{12}   {13}{14}",
                                           (char)frame[0], (char)frame[1], (char)frame[2], (char)frame[3], (char)frame[4], (char)frame[5], (char)frame[6],
                                           (char)frame[7], (char)frame[8], (char)frame[9], (char)frame[10], (char)frame[11], (char)frame[12], (char)frame[13], (char)frame[14]);
                            return modbusHeader;
                        }
                        //Exceptions
                        else
                        {
                            modbusHeader = String.Format("{0}{1}{2}   {3}{4}   {5}{6}   {7}{8}",
                                (char)frame[0], (char)frame[1], (char)frame[2], (char)frame[3], (char)frame[4], (char)frame[5], (char)frame[6], (char)frame[7], (char)frame[8]);
                            return modbusHeader;
                        }
                }
            }
            catch (Exception Ex)
            {
                if (sendInfoEv != null)
                    sendInfoEv(this, new ProjectEventArgs(frame, DateTime.Now, "ModbusMasterASCII.FormatFrameResponse :" + Ex.Message));

                return "Fault: " + frame.Aggregate("", (sum, next) => sum + " " + next.ToString());
            }
        }

        /// <summary>
        /// Zapytanie jednostkowe bajtami danych
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        byte[] IDriverModel.sendBytes(byte[] data)
        {
            return null;
        }

        /// <summary>
        /// Kalkulacja LRC
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public byte calculateLRC(byte[] bytes)
        {
            byte lr = 0;
            for (int i = 0; i < bytes.Length; i++)
                lr = (byte)(lr + bytes[i]);

            return (byte)(-lr);
        }

        /// <summary>
        /// Sterownik zajety
        /// </summary>
        bool IDriverModel.isBusy
        {
            get { return bWorker.IsBusy; }
        }

        /// <summary>
        /// Rone parametry
        /// </summary>
        bool[] IDriverModel.AuxParam
        {
            get { return new Boolean[] { true, false }; }
        }

        //Do wyswietlania
        public override string ToString()
        {
            return "Driver: " + ((IDriverModel)this).driverName;
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    sPort.Dispose();
                    bWorker.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion IDisposable Support
    }
}