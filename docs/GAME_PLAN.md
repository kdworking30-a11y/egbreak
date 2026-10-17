# EGG — Kế hoạch game tổng thể

Ngày cập nhật: 17/10/2026  
Trạng thái: định hướng thiết kế và kế hoạch phát triển; các giá trị cân bằng mới bên dưới là đề xuất để playtest, chưa phải tính năng đã triển khai.

## 1. Ý tưởng và trải nghiệm chính

Một game mobile dọc kết hợp **đập trứng có cảm giác chất liệu**, **sưu tập toy** và **thu nhập idle**.

Người chơi mua một quả trứng, chạm để làm nứt và bóc vỡ từng cụm vỏ, khám phá toy bên trong rồi đưa toy về garden. Toy tự tạo coin để mua trứng tiếp theo. Trứng mới đem đến chất liệu mới, bộ toy mới và tốc độ kiếm tiền cao hơn.

Cảm xúc cần tạo:

- Trước khi đập: tò mò về toy và muốn thử cảm giác vỡ của chất liệu này.
- Trong khi đập: mỗi chạm có phản hồi rõ; toy lộ dần khiến người chơi muốn hoàn thành.
- Khi mở: nhìn rõ toy, biết đó là toy mới hay duplicate và biết thu nhập tăng bao nhiêu.
- Khi trở về garden: thấy bộ sưu tập và tổng thu nhập lớn lên.

Ba trụ cột thiết kế:

1. **Đập thích tay:** hình ảnh, âm thanh, rung và chuyển động cùng diễn tả một chất liệu.
2. **Toy đáng sưu tập:** dễ nhận diện, có nhóm chủ đề và xuất hiện trong garden.
3. **Tiến trình dễ hiểu:** toy tạo tiền; tiền mua trứng; trứng mở ra toy.

## 2. Những quyết định đã chốt

- **Toy tạo coin/giây. Trứng không trực tiếp tạo coin.**
- Coin dùng để mua trứng; toy mới làm tăng tổng income.
- Trứng dùng mesh đã chia mảnh trước; không cần cắt mesh trực tiếp trên điện thoại mỗi lần chạm.
- Đập theo cụm gần điểm chạm; có mảnh văng/rơi và toy lộ qua phần vỏ đã mất.
- Reward được chọn một lần khi bắt đầu quả trứng, chỉ trao một lần khi hoàn tất.
- Flow kết thúc quả trứng có màn reveal và nút Next.
- Duplicate có ích: thêm sản lượng và tăng sao sưu tập, tối đa 3 sao.
- Unity, URP, giao diện mobile dọc là nền tảng hiện tại.

## 3. Core loop và flow màn hình

### Vòng lặp ngắn

```text
Toy trong Garden tạo coin theo thời gian
→ Chọn và mua Egg
→ Đập / làm nứt / rơi từng cụm mảnh
→ Toy lộ dần
→ Hoàn tất và unlock Toy
→ Cộng toy vào Garden, cập nhật income
→ Reveal: New hoặc Duplicate + sao + mức tăng income
→ Next → quay lại chọn trứng
```

### Vòng lặp dài

```text
Mở thêm trứng
→ Hoàn thành các nhóm toy
→ Tăng income và đạt mốc mở egg tier mới
→ Trải nghiệm chất liệu mới
→ Mở rộng Garden và bộ sưu tập
```

### Các màn hình V1

| Màn hình | Nội dung chính | Hành động chính |
|---|---|---|
| Garden / Home | Coin, tổng income, toy đang sản xuất, tiến trình mở tier | Mua trứng, xem collection |
| Chọn trứng | Giá, chất liệu, bộ toy có thể nhận, điều kiện mở khóa | Mua một trứng |
| Cracking | Trứng lớn giữa màn hình, tiến độ vỡ, toy lộ dần | Chạm vào vỏ |
| Reveal | Toy lớn, New/Duplicate, sao, income trước/sau | Next, xem toy trong collection |
| Collection | Toy đã mở, silhouette còn thiếu, số bản sao, sao | Xem chi tiết, quay lại |
| Offline reward | Thời gian được tính và coin tích lũy | Nhận một lần rồi vào Home |

Trong giai đoạn prototype có thể giữ Home và shop cùng một màn. Không cần tách thêm màn nếu làm chậm thao tác mua và đập.

### Máy trạng thái

- `Home`: được mua trứng nếu đủ tiền và đã mở tier.
- `Cracking`: giữ một quả đang đập; khóa mua thêm quả khác.
- `Reveal`: kết quả đã được cộng; chạm thêm không phát sinh reward.
- `Next`: quay về Home; không tự trừ tiền mua quả tiếp theo.
- `Collection`: là lớp UI phủ lên trạng thái hiện tại; không reset trứng.

Coin vẫn được tạo khi đang đập, xem reveal hoặc xem collection. Khi rời app, tiến trình dang dở cần được lưu để tiếp tục đúng quả trứng đó.

## 4. Chất liệu trứng — ưu tiên lớn nhất của bản tiếp theo

### Vấn đề hiện tại

Các tier hiện chủ yếu khác màu. Fracture đã hoạt động nhưng vỏ chưa truyền đạt được sự khác nhau giữa vỏ trứng thường, chocolate và crystal. Chỉ đổi shader hoặc tăng số mảnh sẽ chưa đủ.

### Đặc tả ba chất liệu đầu tiên

| Yếu tố | Vỏ trứng thường | Chocolate | Crystal |
|---|---|---|---|
| Bề mặt nguyên vẹn | Nhám nhẹ, đốm nhỏ, highlight mềm | Nâu, bóng mềm, cảm giác dày và đặc | Trong mờ nhẹ, cạnh bắt sáng, mặt phẳng phản chiếu |
| Mặt gãy | Lớp trong trắng ngà, mép mỏng | Mặt cắt nâu khác độ đậm, mép dày | Mặt gãy sáng, góc sắc, highlight ở cạnh |
| Dấu chạm | Nứt mảnh lan ngắn quanh điểm chạm | Vết sứt và đường gãy tương đối rộng | Vết nứt sắc, chớp sáng ngắn tại điểm chạm |
| Mảnh vỡ | Mảnh nhẹ, cong, văng ngắn | Cục nặng hơn, ít bay xa | Mảnh góc cạnh, xoay và lóe sáng |
| Âm thanh | “Tách / rắc” khô, nhỏ | “Cốc / bụp” trầm và đặc | “Ting / rắc” sáng, đuôi ngắn |
| Rung | Nhẹ và ngắn | Nặng hơn, gọn | Sắc, ngắn; nhấn rõ ở cú cuối |
| Reveal cuối | Vỏ bung nhẹ | Cụm vỏ nứt rời với cảm giác có trọng lượng | Burst mảnh và ánh sáng ngắn, không che toy |

Nguyên tắc triển khai:

- Tách material bề mặt ngoài và material mặt cắt; dùng submesh sẵn có khi phù hợp.
- Hình dạng và bề dày mảnh phải hỗ trợ chất liệu. Nếu mesh gốc không đủ mô tả vỏ mỏng hoặc cạnh crystal, cần làm asset riêng cho tier đó.
- Dùng normal/roughness, ánh sáng và phản hồi va đập cùng nhau. Không dùng màu làm dấu hiệu duy nhất.
- Crystal cần thử giải pháp trong mờ nhẹ trước. Không bắt buộc khúc xạ vật lý nếu làm giảm khả năng nhìn trứng hoặc hiệu năng mobile.
- Mỗi cú chạm có phản hồi tức thì; không khóa input bằng animation dài.
- Âm thanh có vài biến thể để tránh lặp cứng. Có nút tắt âm thanh và rung.

### Tiêu chí đạt cho chất liệu

- Người thử có thể phân biệt ba loại mà không nhìn tên tier.
- Nhìn ảnh đứng vẫn thấy khác biệt về độ nhám, độ dày và mặt gãy.
- Khi tắt hình và nghe âm thanh, cảm giác nhẹ / đặc / sắc vẫn khác nhau.
- Toy vẫn là tâm điểm cuối màn reveal; bụi, sáng và mảnh vỡ không che toy quá lâu.
- Đo trên điện thoại mục tiêu, đặc biệt cảnh crystal có nhiều mảnh và hiệu ứng.

## 5. Thiết kế thao tác đập trứng

### Nhịp một quả trứng

1. Trứng xuất hiện rõ chất liệu, kèm phản hồi nhỏ khi mua thành công.
2. Cú chạm đầu cho thấy một dấu nứt/sứt và làm rời một phần vỏ.
3. Các cú tiếp theo mở rộng vùng vỡ quanh điểm chạm, lộ thêm toy.
4. Cú cuối giải phóng phần vỏ còn lại và chuyển sang reveal.
5. Người chơi có đủ thời gian nhìn toy trước khi bấm Next.

Không bắt buộc người chơi phải tìm một mảnh nhỏ cuối cùng. Nếu chỉ còn ít mảnh hoặc đã đủ số hit, gom chúng vào cú kết thúc.

### Số mảnh và số lần chạm là hai thông số độc lập

- Số mảnh quyết định độ chi tiết của fracture.
- Số hit quyết định thời gian và công sức mở một quả.
- Nhiều mảnh không có nghĩa là phải chạm hàng chục lần.

| Tier | Hiện tại | Đề xuất để playtest bản kế tiếp |
|---|---|---|
| Basic | 52 mảnh, 15 hit | Giữ 52 mảnh, thử 6 hit |
| Chocolate | 101 mảnh, 33 hit | Giữ 101 mảnh, thử 9 hit |
| Crystal | 200 mảnh, 66 hit | Giữ 200 mảnh, thử 12 hit |

Mục tiêu thử nghiệm: khoảng 5–12 giây thao tác cho một quả đầu game. Nếu tier cao cần lâu hơn, tăng tính đa dạng của phản hồi trước khi tăng số hit. Các con số đề xuất chưa thay đổi trong bản Unity hiện tại.

V1 dùng tap làm thao tác chính. Drag bóc vỏ, búa, projectile, slime và nhiều lớp vỏ sẽ xét sau khi vòng lặp cơ bản đủ hấp dẫn.

## 6. Toy, Collection và Garden

### Vai trò của toy

Mỗi toy có ID ổn định, tên, artwork/prefab, nhóm sưu tập và income cơ bản. Toy mới vừa là phần thưởng khám phá vừa là một đơn vị sản xuất.

Định hướng nội dung V1: **12 toy, 3 nhóm, mỗi nhóm 4 toy**. Đây là mục tiêu nội dung mới; bản hiện tại có 4 toy.

| Nhóm / egg pool | Khoảng income đề xuất | Số toy V1 |
|---|---:|---:|
| Basic / Cotton | 1–4 coin/giây | 4 |
| Chocolate / Food | 5–10 coin/giây | 4 |
| Crystal / Special | 12–25 coin/giây | 4 |

Chất liệu vỏ và chủ đề toy có liên hệ để tạo kỳ vọng, nhưng không cần mọi toy có cùng chất liệu với vỏ.

### Duplicate và sao

Giữ quy tắc đơn giản cho V1:

- Bản đầu tiên: unlock toy, 1 sao và một phần income.
- Bản thứ hai: 2 sao và thêm một phần income cơ bản.
- Bản thứ ba: 3 sao và tiếp tục cộng income.
- Từ bản thứ tư: sao giữ ở 3; số lượng và income vẫn tăng.

Sao biểu thị tiến trình sưu tập. **Không nhân thêm income theo sao trong V1**, để tránh vừa cộng bản sao vừa nhân sao làm tăng tiền ngoài dự kiến.

```text
Income của một loại toy = số bản sở hữu × income cơ bản
Tổng income = tổng income của tất cả loại toy
```

Merge, nâng cấp hoặc đổi duplicate thành shard là hướng mở rộng; chưa thêm cùng lúc vào V1.

### Phân phối reward

V1 giữ shuffle bag theo từng egg pool để bảo đảm có sự luân phiên nội dung. Không thêm rarity có xác suất phức tạp ở giai đoạn này. UI cần cho biết pool toy của một loại trứng; sau khi hết một vòng bag, duplicate tiếp tục có ích.

Nếu mở rộng sang rarity/weighted drops, phải thiết kế lại cả thông tin xác suất và cách bảo đảm tiến trình collection; đó là thay đổi riêng.

### Garden cần truyền đạt việc toy đang làm tiền

- Hiện toy hoặc các nhóm toy trực tiếp, có idle motion nhỏ.
- Mỗi loại có số lượng và tổng sản lượng của loại đó.
- Khi nhận toy, toy bay/di chuyển về vị trí tương ứng; con số income tăng ngay.
- Có hiệu ứng coin nhẹ theo nhịp; coin được cộng tự động, không bắt buộc tap collect.
- Duplicate gộp thành stack để màn hình không đầy hàng trăm đối tượng.
- Collection hiển thị toàn bộ danh mục; Garden tập trung vào các toy đang có.

Bản đầu có thể dùng artwork 2D trên quad như hiện tại. Toy 3D chỉ làm nếu mang lại cải thiện trải nghiệm đủ rõ.

## 7. Economy và progression

### Nguyên tắc

- Coin chỉ đến từ toy và phần thưởng coin được thiết kế riêng nếu bổ sung sau này.
- Không thêm coin trực tiếp vào mỗi mảnh vỡ để tránh làm mờ vai trò của toy.
- Tier tốt hơn cần vừa có nội dung mới vừa có lợi ích kinh tế rõ.
- Đầu game ít chờ, hiểu được ngay tác dụng của toy.
- Không dùng lượng hit quá cao để thay cho cân bằng giá.

### Cấu hình khởi đầu đề xuất

Giữ mốc giá prototype để bắt đầu cân bằng: Basic 10, Chocolate 50, Crystal 200 coin. Khi tăng danh mục lên 12 toy và thay đổi nhịp mở, cần đo lại giá; không mặc định ba mốc này sẽ phù hợp đến bản phát hành.

Khởi đầu: 1 starter toy tạo 1 coin/giây và 10 coin để mua quả Basic đầu tiên ngay.

Gates đề xuất để thử:

- Basic: mở từ đầu.
- Chocolate: sau 5 quả đã mở và tổng income ít nhất 6/giây.
- Crystal: sau 12 quả đã mở và đã sở hữu ít nhất một toy thuộc pool Chocolate.

Các gate này chưa có trong MVP. Hiển thị rõ điều kiện và tiến độ; khi đã mở khóa thì không khóa lại. Không yêu cầu sưu tập đủ toàn bộ pool để đi tiếp.

### Mục tiêu 10 phút đầu

| Khoảng thời gian mục tiêu | Trải nghiệm cần đạt |
|---|---|
| 0–30 giây | Hiểu toy tạo tiền, mua và mở được quả đầu tiên |
| 30 giây–2 phút | Nhận vài toy/duplicate, thấy income tăng, biết mở collection |
| 2–5 phút | Mở Chocolate, nhận ra chất liệu khác ngay từ những cú đầu |
| 5–10 phút | Tiến gần hoặc mở Crystal, có lý do muốn quay lại hoàn thành bộ toy |

Đây là mục tiêu trải nghiệm, không phải kết quả mô phỏng đã được chứng minh. Đo bằng phiên chơi thực và chỉnh giá, gate, số hit, pool cùng nhau.

Công cụ cân bằng tối thiểu: bảng giá trứng, income từng toy, thứ tự bag, coin trước/sau mua, thời gian đủ tiền và số trứng mở trong 10 phút. Cần phát hiện việc chỉ mua Basic có lợi hơn mọi tier khác hoặc việc người chơi hết mục tiêu quá sớm.

## 8. Save, offline và vòng quay trở lại

Ưu tiên cho V1 sau khi cảm giác đập ổn:

- Lưu coin, số bản từng toy, collection, tier đã mở và số trứng đã hoàn thành.
- Lưu trứng đang đập: egg ID, reward đã chọn, số hit và trạng thái mảnh còn gắn.
- Lưu tại các giao dịch quan trọng, định kỳ, và khi app pause/thoát.
- Dùng timestamp UTC để tính thời gian vắng mặt.
- Đề xuất offline income 100% sản lượng hiện có, giới hạn 4 giờ để thử nghiệm.
- Offline reward chỉ nhận một lần; thay đổi đồng hồ bất thường không được tạo số tiền âm hoặc cấp tiền lặp vô hạn.
- Reveal bị ngắt giữa chừng phải phục hồi được mà không cộng toy lần hai.

Lý do quay lại đến từ toy muốn sưu tập, coin đủ mua tier mới và garden tiến triển. Chưa cần daily quest, streak hoặc sự kiện trước khi các lý do này đủ mạnh.

## 9. Định hướng kỹ thuật và hiệu năng

Tiếp tục dùng các component hiện có:

| Phần | Trách nhiệm |
|---|---|
| `ToyDefinition` | Dữ liệu và hình ảnh toy, income cơ bản |
| `EggDefinition` | Giá, pool reward, prefab fracture, fragment count, hits |
| `EconomyManager` | Wallet và giao dịch |
| `ToyGarden` | Số toy sản xuất và tổng income |
| `EggShop` | Điều kiện mua và mở tier |
| `EggCrackController` | Mảnh còn gắn, chọn cụm theo điểm chạm, detach và kết thúc |
| `GameLoopController` | Chuyển trạng thái và giao reward một lần |
| `HUD` | Coin, income, shop, reveal, Next và collection |
| `ToyCollection` | ID toy, số bản và sao |

Bổ sung theo nhu cầu:

- `EggMaterialProfile`: material ngoài/trong, texture, tham số nứt, âm thanh, rung và đặc tính debris cho từng chất liệu.
- `SaveSystem`: snapshot có version và phục hồi giao dịch dang dở.
- Dữ liệu unlock tier trong ScriptableObject; tránh rải điều kiện ở nhiều UI.

Ràng buộc mobile:

- Prefab fracture và mesh được chuẩn bị trước; dùng lại shared mesh/material khi có thể.
- Giới hạn số Rigidbody đang sống; tái sử dụng debris nếu profiling cho thấy cần.
- Giữ thời gian sống debris ngắn. Hiện tại là 2 giây, không va chạm lẫn nhau.
- Giới hạn transparency, particle và overdraw của Crystal.
- Safe area, nút lớn, đọc được ở màn hình nhỏ; feedback không dựa riêng vào màu hay âm thanh.
- Mục tiêu 60 FPS trên thiết bị mục tiêu được chọn; đo cả frame time xấu, bộ nhớ và nhiệt khi chơi liên tục. Chưa coi test Editor là bằng chứng hiệu năng điện thoại.

## 10. Hiện trạng đã làm và khoảng trống

### Đã có

- MVP playable trong Unity URP, scene `Assets/ToyEconomy/Scenes/ToyEconomy.unity`.
- Toy sinh coin, mua trứng, đập, reveal, Next và collection.
- Mesh lấy từ `imageai`: tier tên 50/100/200 thực tế có 52/101/200 mảnh.
- Cấu hình đang chạy: 15/33/66 hit, giá 10/50/200.
- 4 artwork toy gốc: Cotton Bunny, Cotton Dino, Toast Cat, Raincoat Cat.
- Chọn reward trước, phá cụm gần điểm chạm, debris có vật lý và tự dọn.
- Duplicate cộng income, sao capped 3; collection có trạng thái chưa unlock.
- 417 kiểm tra Play Mode đã qua trên bản sao kiểm tra; scene sau đó đã được mở chạy trong Editor chính.

### Chưa đạt / chưa có

- Vật liệu chưa diễn tả rõ vỏ mỏng, chocolate dày hoặc crystal.
- Chưa có hệ âm thanh/rung hoàn chỉnh và nứt bề mặt theo từng chất liệu.
- Garden hiện vẫn là danh sách sản lượng; chưa có bố cục toy sống động.
- Chưa có save/load, offline income, unlock gates và bộ nội dung 12 toy.
- Chưa xác nhận bằng build và profiling trên Android/iOS.

Project tham chiếu `imageai` được dùng để lấy asset và học flow; các thay đổi game nằm trong project MVP hiện tại.

## 11. Roadmap theo thứ tự ưu tiên

Không ấn định ngày hoàn thành trước khi thử asset và đo trên thiết bị. Mỗi chặng có đầu ra để chơi và tiêu chí nghiệm thu riêng.

### P0 — Làm chất liệu và cảm giác đập thật sự khác nhau

1. Làm mẫu một quả Basic: vỏ ngoài, mặt gãy, bề dày, âm thanh và rung.
2. Dùng mẫu đó để hoàn thiện Chocolate và Crystal với đặc tính riêng.
3. Thử 6/9/12 hit với số mảnh hiện có.
4. Hoàn thiện dấu nứt, cú cuối và reveal ngắn.
5. Chơi mù tên tier và đo hiệu năng một cảnh nhiều mảnh.

**Đạt khi:** ba chất liệu nhận ra được bằng cảm giác nhìn/nghe/chạm; nhịp đập không gây mỏi; không có lỗi reward hoặc mảnh còn sót cản hoàn thành.

### P1 — Làm rõ giá trị của toy và tiến trình

1. Garden có hình toy/stack, income từng loại và phản hồi tăng income.
2. Màn reveal hiển thị New/Duplicate, số bản, sao và tổng income trước/sau.
3. Mở rộng 12 toy, pool rõ ràng và collection có mục tiêu hoàn thành.
4. Thêm unlock gates; cân bằng 10 phút đầu.

**Đạt khi:** người mới hiểu “toy đang làm tiền cho mình”, muốn mở thêm bộ toy và nhìn thấy mục tiêu tiếp theo mà không cần giải thích bên ngoài.

### P2 — Có thể quay lại chơi lâu dài

1. Save/load, phục hồi quả đang đập và chống cấp reward lặp.
2. Offline income, giới hạn và màn nhận thưởng.
3. Kiểm tra pause/resume, thoát cưỡng bức, đổi phiên bản save.
4. Build trên điện thoại, sửa safe area và tối ưu hiệu năng.

**Đạt khi:** tiến trình không mất, thu nhập được tính đúng và vòng lặp vẫn ổn khi chơi nhiều phiên.

### P3 — Mở rộng sau khi playtest xác nhận core tốt

Cân nhắc lần lượt: bộ toy/chất liệu mới, trang trí garden, nâng cấp có lựa chọn, merge hoặc shard duplicate, event theo mùa, drag bóc vỏ hoặc nhiều lớp trứng. Mỗi hệ thống phải giải quyết một nhu cầu đã quan sát được.

Monetization chưa phải ưu tiên prototype. Nếu thêm quảng cáo thưởng, nên là lựa chọn bổ sung; người chơi vẫn đi trọn core loop khi không xem quảng cáo. Chưa đưa mua trứng bằng tiền thật vào phạm vi V1.

## 12. Checklist nghiệm thu và câu hỏi playtest

### Chức năng

- Mua đúng giá, thiếu tiền không bị trừ, đang đập không mua thêm.
- Mỗi chạm xử lý đúng số mảnh; tổng số mảnh khớp prefab thật.
- Reward được chọn một lần và được trao đúng một lần.
- Duplicate tăng đúng số bản và income; sao không vượt 3.
- Next không tự mua trứng, không cộng thưởng thêm.
- Collection không làm mất quả đang đập.
- Debris, material và hiệu ứng được dọn; không tăng bộ nhớ qua nhiều quả.
- Khi có save: pause/thoát ở mọi trạng thái đều phục hồi đúng.

### Cảm giác chơi

- Người thử nhận ra chất liệu nào trước khi thấy tên?
- Cú chạm nào thỏa mãn nhất, cú nào không có cảm giác tác động?
- Toy lộ dần có khiến họ muốn hoàn thành không?
- Bao nhiêu hit bắt đầu gây mỏi hoặc thấy lặp?
- Sau reveal, họ có hiểu income tăng bao nhiêu không?
- Duplicate có còn đáng nhận sau 3 sao không?
- Sau 10 phút, họ muốn chơi tiếp vì toy, chất liệu hay chỉ vì số tiền?
- Có thời điểm chỉ chờ tiền mà không có quyết định hoặc mục tiêu nào không?

### Ghi nhận khi test

Theo dõi thời gian mua/mở từng quả, số hit, tier, reward ID, new/duplicate, income trước/sau, thời gian chờ đủ tiền, lúc mở collection, lúc dừng phiên và lỗi runtime. Prototype có thể ghi log cục bộ; chưa cần tích hợp dịch vụ analytics.

## 13. Việc nên làm ngay tiếp theo

**Chọn một quả Basic làm chuẩn chất lượng**, hoàn thiện bề mặt, mặt cắt, bề dày mảnh và phản hồi âm thanh/rung; sau đó mở rộng sang Chocolate và Crystal. Chơi thử ba chất liệu trước khi bổ sung hệ thống progression mới.

Plan này thay thế cách đánh giá tiến độ chỉ bằng “đã có fracture và đủ số mảnh”. Tiêu chí của bản kế tiếp là người chơi cảm nhận được chất liệu và thấy toy nhận được làm garden tốt lên.

## 14. Mở rộng: mỗi vật liệu có gameplay riêng

Bổ sung ngày 17/10/2026 theo trao đổi về gameplay chất liệu. Đây là thiết kế mở rộng, chưa được implement và chưa đưa toàn bộ vào phạm vi V1. Phần này phát triển thêm các mô tả hình ảnh/âm thanh ở mục 4.

### 14.1. Vật liệu là một vòng tương tác nhỏ

Mỗi vật liệu cần có bốn thứ: **thao tác đặc trưng → phản ứng dễ hiểu → tiến độ lộ toy → khoảnh khắc mở hoàn toàn**.

Không dùng cùng một cơ chế tap rồi chỉ đổi màu và số hit. Cũng không biến mỗi quả thành một minigame dài phải học lại từ đầu. Người chơi dùng một ngón tay, học luật bằng phản ứng của quả trứng, và hoàn thành trong khoảng thời gian tương đương.

Quy tắc chung:

- Tương tác với vỏ không tạo coin; toy nhận được vẫn là nguồn income.
- Reward được chọn trước khi tương tác. Kỹ năng không thay đổi xác suất nhận toy hoặc làm mất reward.
- Làm chưa đúng chỉ chậm hơn; không mất trứng, không mất thêm coin, không bị reset toàn bộ tiến độ.
- Mỗi vật liệu có một động từ chính, một dấu hiệu hướng dẫn và một phản hồi rõ.
- Có chế độ thao tác đơn giản: tap hoặc nút giữ thay cho timing/drag nếu người chơi cần.
- Không bắt buộc dùng rung, âm thanh hoặc nhận biết màu để hoàn thành.

### 14.2. Danh mục vật liệu

| Vật liệu | Động từ chính | Luật gameplay | Cảm giác hoàn tất | Ưu tiên |
|---|---|---|---|---|
| Vỏ trứng thường | Gõ | Chạm mở rộng vết nứt và làm rời cụm vỏ gần đó | Mảnh vỏ mỏng bung ra | Chuẩn chất lượng đầu tiên |
| Chocolate | Ấn–bẻ | Giữ tại một vùng để tích lực; thả để bẻ một mảng | Miếng chocolate dày gãy rời | Thử nghiệm đầu |
| Băng | Miết tan | Kéo ngón tay làm tan lớp sương/băng tại vùng đi qua; vùng đủ mỏng nứt ra | Băng trong dần, nứt và trượt xuống | Thử nghiệm đầu |
| Gỗ | Bổ theo thớ | Vuốt dọc hướng thớ để tạo đường tách; vài đường tách mở vỏ | Hai hoặc ba miếng gỗ lớn tách đôi | Mở rộng sau thử nghiệm |
| Crystal | Gõ cộng hưởng | Gõ vào điểm đang lóe; gõ khi vòng sáng mở rộng tạo vết nứt lan xa hơn | Chuỗi mặt tinh thể vỡ với âm sáng | Mở rộng sau thử nghiệm |
| Jelly / slime | Kéo–thả | Kéo vỏ đàn hồi ra khỏi toy rồi thả để bóc một phần vỏ | Jelly tuột khỏi toy và nảy nhẹ | Thử nghiệm riêng |
| Gốm tráng men | Gõ–tách theo vùng | Gõ vùng có đường men yếu; vùng vỡ mở đường tiếp cận vùng kế tiếp | Mảng gốm sứt rồi đổ xuống | Sau V1 |
| Kim loại ghép tấm | Nạy–kéo | Chạm làm bật mép/ngàm rồi kéo tấm theo hướng mở | Các tấm bung khỏi ngàm, lộ toy | Sau V1 |

### 14.3. Chocolate — tích lực rồi bẻ

**Micro loop:** đặt ngón tay → vùng chocolate lún nhẹ/đường gãy xuất hiện → lực đạt mức đủ → thả → một miếng dày gãy.

- Giữ khoảng 0,4–0,8 giây cho một miếng là điểm bắt đầu để thử, chưa phải thông số chốt.
- Lực tăng được diễn tả ngay trên vùng ấn bằng độ lún và đường gãy; không cần thanh năng lượng lớn che màn hình.
- Thả sớm vẫn tạo tiến độ nhỏ, không xóa lực đã tác động lên vùng đó.
- Giữ đủ lâu tự giới hạn lực; không có phạt “quá lực”.
- Khoảng 3–5 miếng lớn cho một quả. Mỗi miếng có thể gồm nhiều mesh nhỏ để giữ vẻ fracture.
- Mặt cắt dày, văng ít, âm gãy trầm và rung gọn.

**Điểm khác Basic:** Basic phản hồi từng tap; Chocolate cho người chơi cảm giác vật liệu chống lại lực trước khi gãy.

### 14.4. Băng — miết làm mỏng rồi nứt

**Micro loop:** kéo ngón tay → sương mờ biến mất, lộ băng trong và toy → một vùng đủ mỏng → vùng băng nứt/rời.

- Dùng một lớp mask chia vùng để theo dõi tiến độ tan; không cần mô phỏng nhiệt hay nước thật.
- Mỗi vùng có giới hạn tiến độ theo thời gian để việc miết cực nhanh không trở thành cách duy nhất tối ưu.
- Khi không chạm, vùng đã tan không tự đóng băng trở lại trong V1.
- Những mảng băng còn lại tự kết thúc khi đạt ngưỡng diện tích đã xử lý, tránh bắt người chơi chà vào một góc rất nhỏ.
- Vỏ trong dần, có giọt nước và âm tan/nứt nhẹ. Một ít mảnh băng trượt xuống ở cuối.

**Điểm khác Basic:** mục tiêu là xử lý diện tích bề mặt, không đếm số hit. Người chơi dần nhìn rõ toy trước khi toàn bộ vỏ biến mất.

### 14.5. Gỗ — đọc thớ và tạo đường tách

**Micro loop:** thấy thớ gỗ → vuốt theo thớ → đường tách dài ra → vài đường tách nối nhau → miếng vỏ mở ra.

- Dùng đường thớ lớn và hướng dẫn ngắn bằng chuyển động mẫu, không chỉ bằng mũi tên hoặc màu.
- Chấp nhận góc vuốt rộng; không yêu cầu vẽ đường chính xác theo pixel.
- Vuốt lệch vẫn gây sứt nhỏ và gợi lại hướng thớ; không xóa tiến độ.
- Mỗi quả có khoảng 2–4 đường tách chính để thử. Không đưa mọi đường nối của mesh thành mục tiêu riêng.
- Mảnh dài, có dằm nhỏ trang trí, âm răng rắc; cú cuối mở thành hai hoặc ba phần lớn.

**Điểm khác Basic:** vị trí và hướng vuốt quan trọng hơn tốc độ chạm.

### 14.6. Crystal — nhịp cộng hưởng dễ tiếp cận

**Micro loop:** một điểm trên crystal lóe → vòng sáng mở rộng → người chơi gõ → vết nứt lan và một cụm mặt vỡ.

- Gõ bình thường luôn có tiến độ. Gõ gần nhịp sáng chỉ giúp phá nhiều hơn và có phản hồi đẹp hơn.
- Không có màn thất bại, mất combo làm mất tiền, hoặc yêu cầu nghe âm thanh mới chơi được.
- Cửa sổ nhịp rộng, có thể giữ cùng vị trí chạm; không ép đuổi theo nhiều điểm nhỏ.
- Phản hồi gồm cạnh sáng, âm “ting” ngắn và đường nứt nối các mặt.
- Có tùy chọn tắt nhịp, chuyển thành tap phá cụm như Basic.

**Điểm khác Basic:** người chơi có thể tận hưởng nhịp và tối ưu cú gõ, nhưng vẫn hoàn thành nếu không chơi theo nhịp.

### 14.7. Jelly / slime — đàn hồi và bóc vỏ

**Micro loop:** giữ một phần jelly → kéo làm vỏ giãn ra → đường bám quanh toy mỏng dần → thả/bóc rời một phần.

- Hình ảnh phải cho thấy ngón tay đang nắm phần nào, độ kéo và điểm sắp tuột.
- Giới hạn độ giãn để jelly không biến dạng vô nghĩa hoặc che kín UI.
- Kéo ngắn rồi thả tạo tiến độ nhỏ; kéo đủ sẽ bóc được một phần lớn.
- Dùng các vùng bóc được định sẵn và biến dạng có kiểm soát, chưa cần soft-body physics đầy đủ.
- Cú cuối là jelly tuột khỏi toy, không dùng cùng burst mảnh sắc như Crystal.

**Điểm khác Basic:** lớp vỏ giữ tính liên tục và co giãn, thay vì chỉ bể thành các mảnh cứng. Đây là một nhánh kỹ thuật riêng; cần prototype nhỏ trước khi cam kết đưa vào game.

### 14.8. Gốm và kim loại — ý tưởng cho chặng sau

**Gốm:** người chơi nhận biết các vùng men yếu lớn. Gõ làm sứt một vùng, mở chỗ để tách vùng liền kề. Chuỗi vùng ngắn, có thể chọn nhiều hướng, tránh thành bài nhớ thứ tự. Phải tạo được cảm giác men ngoài bóng và phần gốm bên trong nhám; nếu playtest vẫn giống Basic thì chưa cần thêm tier này.

**Kim loại:** thiết kế như vỏ đồ chơi ghép tấm với ngàm, không giả kim loại vỡ giòn. Người chơi bật một ngàm rồi kéo tấm vỏ theo hướng bản lề/đường ghép. Mặt kim loại móp nhẹ, có âm cạch và âm tháo ngàm. Một quả chỉ có vài tấm lớn. Dùng animation/pivot được chuẩn bị trước cho tấm vỏ.

### 14.9. Tách chất liệu khỏi giá trị kinh tế của egg

Đề xuất dữ liệu dài hạn:

```text
Egg = Material + Toy Pool + Price + Unlock Condition
Material = Interaction + Surface/Interior + Feedback + Reveal Style
```

Chất liệu quyết định **cách mở**, còn pool và giá quyết định **vai trò progression**. Không mặc định vật liệu mở lâu nhất luôn chứa toy mạnh nhất.

Trong prototype, giữ ba SKU hiện tại để giảm khối lượng thay đổi. Khi đã có ít nhất ba kiểu tương tác tốt, thử cho cùng một nhóm toy xuất hiện trong hai chất liệu tương đương giá để người chơi chọn cảm giác mình thích. Nếu thời gian mở chênh lệch lớn, cân bằng lại trải nghiệm hoặc giá, không âm thầm giảm reward của người thích một kiểu thao tác.

Không tạo cây công cụ bắt buộc cho từng chất liệu ở V1. Người chơi không cần mua thêm búa mới có thể mở một quả đã trả tiền.

### 14.10. Chuẩn hóa tiến độ và lưu game

Các vật liệu có cùng hợp đồng gameplay nhưng khác cách tạo tiến độ:

| Phần chung | Dữ liệu riêng theo vật liệu |
|---|---|
| Một egg đang hoạt động, một reward đã chọn | Hit/vùng nứt của Basic |
| Tiến độ mở từ 0 đến 1, phần vỏ còn tồn tại | Lực tích lũy và vùng đã bẻ của Chocolate |
| Event feedback, progress và completed một lần | Mask/vùng tan của Băng |
| Chặn mua trứng mới trong lúc đang mở | Các đường tách của Gỗ |
| Reveal và chuyển Next thống nhất | Mặt đã vỡ của Crystal |
| Snapshot để tiếp tục khi vào lại app | Vùng đã bóc của Jelly; trạng thái tấm/ngàm của Metal |

Không ép mọi vật liệu vào một biến `hitsToBreak`. Với vật liệu không dùng hit, lưu trạng thái đủ để dựng lại bề mặt và tiếp tục đúng tiến độ.

Tạm dừng khi đang giữ/kéo: giữ phần tiến độ đã xác nhận, hủy gesture hiện tại, phục hồi về tư thế nghỉ ổn định. Khi tiếp tục, không tự thực hiện thêm một cú bẻ hoặc cấp thêm reward.

### 14.11. Lộ trình thử nghiệm gameplay chất liệu

1. **Basic:** hoàn thiện tap fracture làm mốc so sánh cho hình, âm và rung.
2. **Chocolate:** thử ấn–bẻ để có tương tác khác bằng một thay đổi vừa phải.
3. **Băng:** thử miết tan để so sánh thao tác theo diện tích với thao tác theo lực.
4. Chơi thử ba mẫu với cùng toy reward và thời gian mở gần nhau, để đo sức hút của thao tác.
5. Chọn kiểu được người thử muốn chơi lại rồi mới mở rộng Wood/Crystal.
6. Jelly là thử nghiệm riêng; Gốm và Metal đợi đến khi có bằng chứng cần thêm nội dung.

Crystal vẫn nằm trong bộ ba hình ảnh/chất liệu đã định hướng ở mục 4. Thứ tự ở đây là **thứ tự thử cơ chế tương tác mới**, không tự thay thế các tier đang chạy hoặc thay roadmap V1 thành tám minigame.

### 14.12. Tiêu chí quyết định giữ một vật liệu

- Người chơi mô tả được thao tác của nó bằng một động từ riêng.
- Hiểu cách chơi sau một lần hướng dẫn ngắn bằng hình động.
- Cảm thấy hành động của mình gây ra thay đổi trực tiếp trên vật liệu.
- Có tiến độ và phần thưởng rõ, không mắc kẹt vào mảnh/vùng rất nhỏ.
- Muốn mở lại vài quả cùng chất liệu mà không thấy đó là thao tác bắt buộc gây mệt.
- Chế độ thao tác đơn giản vẫn cho phép nhận cùng reward.
- Hoạt động ổn trên thiết bị mục tiêu và phục hồi được sau pause/save/load.

Nếu một vật liệu chỉ khác màu, chỉ tăng số hit, hoặc cần hướng dẫn dài mới chơi được thì chưa đủ lý do thêm vào danh mục.

## 15. Cập nhật triển khai gameplay vật liệu — 17/10/2026

Đã triển khai đợt đầu trong Unity: Basic tap 6 hit; Chocolate giữ–thả, khoảng 0,65 giây cho lực đầy, 4 lần bẻ đầy; Ice miết theo vùng để tan. Giữ Crystal với 12 hit và bề mặt bóng riêng, chưa có nhịp cộng hưởng. Ice là một lựa chọn mua thêm giá 100 coin, dùng lại pool toy Crystal hiện có.

Có material bề mặt/mặt cắt riêng, biến dạng nhẹ và vết gãy khi giữ Chocolate, mask tan cục bộ cho Ice, phản hồi âm thanh prototype, chỉ dẫn và thanh tiến độ. SIMPLE cho phép mở bằng 6 tap với cùng reward; SOUND tắt âm. Mở collection hoặc mất focus hủy lần giữ đang dang dở, không tự bẻ thêm.

Đã qua 95 kiểm tra Play Mode trên bản sao kiểm tra, không ghi nhận lỗi runtime. Test có thêm coin để đi nhanh qua từng loại; phiên chơi thông thường vẫn bắt đầu bằng 10 coin và starter toy. Báo cáo/ảnh: `MaterialGameplay/Validation` trong workspace, `Benchmarks/ToyEconomy/Materials` trong Unity project.

Đây là ba cơ chế đầu tiên, chưa phải toàn bộ danh mục mục 14. Gỗ, jelly, gốm, kim loại và Crystal timing chưa triển khai; haptics/save/offline/device profiling vẫn còn. Ice hiện dùng hiệu ứng frost/dissolve trong shader opaque, chưa mô phỏng khúc xạ hoặc nước thật.

## 16. Cập nhật reflection/refraction — 17/10/2026

Đã thay phần render chỉ có highlight của mục 15 bằng môi trường phản chiếu studio HDR và shader kính transparent lấy màu màn hình URP để khúc xạ toy/nền phía sau. Có Fresnel, IOR (băng 1,31; crystal 1,52), tint theo độ dày, roughness và dispersion. Băng dùng frost blur giảm theo độ tan; crystal có phản chiếu mặt cắt sắc nhưng giữ lens normal tương đối liên tục để toy không bị chia vụn. Chocolate phản chiếu môi trường với độ nhám riêng; Basic vẫn nhám.

Đã qua 21 kiểm tra render có đối chiếu ảnh bật/tắt từng hiệu ứng và 95 kiểm tra gameplay với bộ render mới, không ghi nhận lỗi runtime. Đây là phản chiếu môi trường baked cubemap và khúc xạ screen-space; chưa mô phỏng ray tracing hoặc các lớp kính lồng nhau. Vẫn cần đo hiệu năng trên điện thoại.

Ảnh và báo cáo mới: `OpticalMaterials/Validation` trong workspace; `Benchmarks/ToyEconomy/Optics` trong Unity project. Các ghi chú cũ nói “chưa có refraction” được thay thế bởi cập nhật này.

## 17. Idle progression và tool shop đã triển khai (18/10/2026)

Thiết kế và thông số hiện hành nằm ở [IDLE_DESIGN.md](IDLE_DESIGN.md). Hoàn tất 3/6/10 egg mở Chocolate/Ice/Crystal; mỗi 3 egg tăng 10 điểm phần trăm thu nhập vườn (cap +100%). Có búa, găng chocolate và khăn ấm mua bằng coin, nâng tối đa 5 cấp và tự áp dụng. Toy chưa sở hữu được ưu tiên trong pool. Lưu tiến trình local, backup và offline income cap 4 giờ; egg chưa mở xong được hoàn giá mua khi khôi phục.

Đây là bản cân bằng thử, chưa phải kết quả playtest cho độ tuổi cụ thể. Không thêm tiền thật hoặc daily-streak pressure. Còn cần art/model tool, garden trực quan, toy mới, localization và device profiling.
