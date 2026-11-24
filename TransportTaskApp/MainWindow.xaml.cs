using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;

namespace TransportTaskApp
{
    public partial class MainWindow : Window
    {
        private int _n; // Поставщики
        private int _m; // Потребители

        public MainWindow()
        {
            InitializeComponent();
        }

        // 1. Создание отдельных таблиц
        private void BtnCreateTable_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtSuppliers.Text, out _n) && int.TryParse(TxtConsumers.Text, out _m))
            {
                if (_n <= 0 || _m <= 0) return;

                // 1. Таблица тарифов
                DataTable dtTariffs = new DataTable();
                for (int j = 0; j < _m; j++) dtTariffs.Columns.Add($"B{j + 1}", typeof(int));
                for (int i = 0; i < _n; i++)
                {
                    var row = dtTariffs.NewRow();
                    for (int j = 0; j < _m; j++) row[j] = 1;
                    dtTariffs.Rows.Add(row);
                }
                DataGridTariffs.ItemsSource = dtTariffs.DefaultView;

                // 2. Таблица запасов (1 столбец)
                DataTable dtSupply = new DataTable();
                dtSupply.Columns.Add("Запас", typeof(int));
                for (int i = 0; i < _n; i++)
                {
                    var row = dtSupply.NewRow();
                    row[0] = 100; // По умолчанию
                    dtSupply.Rows.Add(row);
                }
                DataGridSupply.ItemsSource = dtSupply.DefaultView;

                // 3. Таблица потребностей (1 строка)
                DataTable dtDemand = new DataTable();
                for (int j = 0; j < _m; j++) dtDemand.Columns.Add($"B{j + 1}", typeof(int));
                var demandRow = dtDemand.NewRow();
                for (int j = 0; j < _m; j++) demandRow[j] = 100; // По умолчанию
                dtDemand.Rows.Add(demandRow);
                DataGridDemand.ItemsSource = dtDemand.DefaultView;

                // Очистка результата
                DataGridResult.ItemsSource = null;
                TxtCost.Text = "";
            }
        }

        // Чтение всех данных с трех таблиц (с защитой от пустых ячеек)
        private (int[,] tariffs, int[] supply, int[] demand) GetData()
        {
            int[,] tariffs = new int[_n, _m];
            int[] supply = new int[_n];
            int[] demand = new int[_m];

            // Читаем тарифы
            DataTable dtTar = (DataGridTariffs.ItemsSource as DataView).Table;
            for (int i = 0; i < _n; i++)
                for (int j = 0; j < _m; j++)
                    tariffs[i, j] = dtTar.Rows[i][j] != DBNull.Value ? Convert.ToInt32(dtTar.Rows[i][j]) : 0;

            // Читаем запасы
            DataTable dtSup = (DataGridSupply.ItemsSource as DataView).Table;
            for (int i = 0; i < _n; i++)
                supply[i] = dtSup.Rows[i][0] != DBNull.Value ? Convert.ToInt32(dtSup.Rows[i][0]) : 0;

            // Читаем потребности
            DataTable dtDem = (DataGridDemand.ItemsSource as DataView).Table;
            for (int j = 0; j < _m; j++)
                demand[j] = dtDem.Rows[0][j] != DBNull.Value ? Convert.ToInt32(dtDem.Rows[0][j]) : 0;

            return (tariffs, supply, demand);
        }

        // Метод Северо-Западного угла
        private void BtnNorthWest_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridTariffs.ItemsSource == null) return;

            var (tariffs, supply, demand) = GetData();
            int[,] plan = new int[_n, _m];

            int i = 0, j = 0;
            long cost = 0;

            int[] s = (int[])supply.Clone();
            int[] d = (int[])demand.Clone();

            while (i < _n && j < _m)
            {
                int minVal = Math.Min(s[i], d[j]);
                plan[i, j] = minVal;
                cost += minVal * tariffs[i, j];

                s[i] -= minVal;
                d[j] -= minVal;

                if (s[i] == 0) i++;
                else if (d[j] == 0) j++;
            }

            ShowResult(plan, tariffs, cost);
        }

        // Метод Минимальных элементов
        private void BtnMinCost_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridTariffs.ItemsSource == null) return;

            var (tariffs, supply, demand) = GetData();
            int[,] plan = new int[_n, _m];
            long cost = 0;

            int[] s = (int[])supply.Clone();
            int[] d = (int[])demand.Clone();

            var cells = new List<(int i, int j, int cost)>();
            for (int i = 0; i < _n; i++)
                for (int j = 0; j < _m; j++)
                    cells.Add((i, j, tariffs[i, j]));

            cells = cells.OrderBy(c => c.cost).ToList();

            foreach (var cell in cells)
            {
                int i = cell.i, j = cell.j;

                if (s[i] > 0 && d[j] > 0)
                {
                    int minVal = Math.Min(s[i], d[j]);
                    plan[i, j] = minVal;
                    cost += minVal * cell.cost;

                    s[i] -= minVal;
                    d[j] -= minVal;
                }
            }

            ShowResult(plan, tariffs, cost);
        }

        // Вывод красивой таблицы с результатами
        private void ShowResult(int[,] plan, int[,] tariffs, long totalCost)
        {
            DataTable dtRes = new DataTable();
            dtRes.Columns.Add("Поставщик", typeof(string));
            for (int j = 0; j < _m; j++) dtRes.Columns.Add($"B{j + 1}", typeof(string));

            for (int i = 0; i < _n; i++)
            {
                var row = dtRes.NewRow();
                row[0] = $"A{i + 1}";
                for (int j = 0; j < _m; j++)
                {
                    // Если везем - пишем "Объем / Тариф", если нет - "0 / Тариф"
                    row[j + 1] = $"{plan[i, j]} / {tariffs[i, j]}";
                }
                dtRes.Rows.Add(row);
            }

            DataGridResult.ItemsSource = dtRes.DefaultView;
            TxtCost.Text = $"Минимальная стоимость перевозки: {totalCost}";
        }
    }
}