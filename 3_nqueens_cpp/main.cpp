#include <iostream>
#include <chrono>

int n;
long long pocetReseni = 0;

void BitmaskSolve(int radek, int sloupce, int diagBottomRight, int diagBottomLeft){
    if (radek == n) {
        pocetReseni++;
        return;
    }

    int volno = ~(sloupce | diagBottomRight| diagBottomLeft) & ((1 << n) - 1);

    while (volno) {
        int misto = volno & -volno;
        volno -= misto;
        BitmaskSolve(radek + 1,
            sloupce | misto,
            (diagBottomRight | misto) << 1,
            (diagBottomLeft | misto) >> 1);
    }
}

int main() {
    std::cout << "N:\t";
    std::cin >> n;

    auto start = std::chrono::high_resolution_clock::now();
    BitmaskSolve(0, 0, 0, 0);
    auto end = std::chrono::high_resolution_clock::now();

    auto duration = duration_cast<std::chrono::microseconds>(end - start);
    std::cout << pocetReseni << ", time:\t" << duration.count() / 1000.0 << "ms" << std::endl;
    return 0;
}