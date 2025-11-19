#include <iostream>
#include <chrono>

constexpr int n = 12;
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
    std::ios_base::sync_with_stdio(false);
    std::cin.tie(nullptr);

    const auto start = std::chrono::system_clock::now();
    BitmaskSolve(0, 0, 0, 0);
    const auto end = std::chrono::system_clock::now();

    auto duration = duration_cast<std::chrono::microseconds>(end - start);
    std::cout << pocetReseni << ", time:\t" << duration.count() / 1000.0 << "ms" << std::endl;
    return 0;
}