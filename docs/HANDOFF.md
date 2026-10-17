# EGG Unity — bàn giao cho Claude

Cập nhật: **17/10/2026**. Đây là bản tóm tắt trạng thái thực tế sau các lượt implement và test; không phải yêu cầu tự động triển khai toàn bộ roadmap.

## Đọc trước

1. File này: biết phần nào đang chạy, phần nào còn là đề xuất.
2. [GAME_PLAN.md](GAME_PLAN.md): thiết kế tổng thể; mục 15–16 ghi các cập nhật đã triển khai.
3. Skill `.claude/skills/egg-unity-dev/SKILL.md` và runbook đi kèm khi làm việc với Editor/test/render.

**Source of truth là Unity project bên dưới.** Các thư mục staging trong workspace Codex là bản chụp từng chặng, không được chép đè cả project từ đó.

## 1. Vị trí và môi trường

| Thành phần | Vị trí |
|---|---|
| Unity project đang phát triển | `/Users/khoa/Documents/GitHub/egbreak` |
| Unity Editor | `/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity` |
| Phiên bản / pipeline | Unity **6000.3.10f1**, **URP 17.3.0**, uGUI, Input System |
| Scene chính | `Assets/ToyEconomy/Scenes/ToyEconomy.unity` |
| Code runtime | `Assets/ToyEconomy/Runtime` |
| Editor setup / test launchers | `Assets/ToyEconomy/Editor` |
| Shader | `Assets/ToyEconomy/Shaders` |
| SO / materials | `Assets/ToyEconomy/Data`, `Assets/ToyEconomy/Materials` |
| Project tham chiếu đã đọc | `/Users/khoa/Documents/GitHub/imageai` |
| Workspace tài liệu ban đầu | `/Users/khoa/.codex/.chatgpt-projects/g-p-6968c7ca5e288191a22d5b55614cc5a8` |

Lần kiểm tra cuối: Editor chính đang **Play Mode, không pause**, không ghi nhận lỗi compile/shader/runtime kể từ upgrade optics. Đây là snapshot; Claude cần kiểm tra lại trạng thái hiện tại trước khi thao tác.

Project `imageai` không bị chỉnh sửa. Scene Egg Lab trước đó vẫn còn trong project chính. Không giả định project đã được commit; chưa tạo commit/PR trong chuỗi công việc này.

## 2. Ý định của người dùng và phản hồi mới nhất

Game mobile dọc, đập trứng mở toy, sưu tập và tăng thu nhập idle.

Người dùng đã chơi MVP và phản hồi hai lần rằng **vật liệu chưa thể hiện được đặc tính**. Sau khi có cơ chế riêng, người dùng tiếp tục chỉ ra thiếu **reflection/refraction**. Vì vậy chất lượng hình ảnh nhìn thấy trong game là tiêu chí quan trọng; chỉ có shader property hoặc số test pass là chưa đủ.

Lượt vừa hoàn tất đã thêm reflection từ môi trường studio và refraction screen-space vào Unity chính, có ảnh A/B xác nhận hiệu ứng. Người dùng **chưa đánh giá lại bản optics này**; sau đó người dùng yêu cầu tiếp tục. Bản polish tiếp theo đã thêm frost theo vùng, vân đông và bọt khí nhỏ cho Ice, giảm dần theo `_Heat`; Crystal giữ nguyên optics. Người dùng chưa duyệt art của bản polish này.

Không tự suy diễn rằng người dùng đã duyệt chất lượng art cuối cùng, hoặc đã yêu cầu implement tất cả vật liệu còn trong roadmap.

## 3. Core loop và các quy tắc phải giữ

```text
Toy tự sinh coin/giây
→ dùng coin mua egg
→ tương tác với vỏ theo vật liệu
→ toy lộ dần
→ hoàn tất: nhận đúng một toy, tăng income
→ Reveal: New / Duplicate, sao
→ Next trở lại shop
```

- **Toy tạo tiền; egg/mảnh vỡ không trực tiếp tạo tiền.**
- Bắt đầu bình thường bằng 10 coin và Cotton Bunny sinh 1 coin/giây.
- Reward được chọn một lần khi mua, không reroll theo thao tác hoặc khi đổi chế độ.
- Chặn mua thêm egg khi đang mở hoặc đang ở màn reward.
- Tương tác sau reveal không được cộng reward thêm; Next không tự mua egg.
- Duplicate: thêm một bản sản xuất; sao collection tăng tối đa 3. Không nhân income thêm lần nữa theo sao.
- Chọn toy bằng shuffle bag riêng theo egg pool; tránh lặp ngay ở ranh giới vòng nếu có lựa chọn khác.
- Coin vẫn sinh khi đang tương tác, xem reveal hoặc collection.
- SIMPLE là lựa chọn 6 tap để mở mọi loại, reward giữ nguyên.

## 4. Nội dung và gameplay hiện đang chạy

| Egg | Giá | Mảnh thật | Cách mở |
|---|---:|---:|---|
| Basic | 10 | 52 | 6 tap, vỡ cụm gần điểm chạm |
| Chocolate | 50 | 101 | Giữ khoảng 0,65 giây để đầy lực, thả để bẻ; 4 lần bẻ đầy từ đầu |
| Crystal | 200 | 200 | 12 tap; chưa có cơ chế nhịp cộng hưởng |
| Ice | 100 | 200 | Miết qua bề mặt để tan theo vùng; tap hoặc giữ đứng yên không đủ |

Chocolate thả sớm vẫn tạo tiến độ nhỏ (`shortPressProgress = 0.06`), không phạt quá lực. Đổi SIMPLE sang lại hold phải giữ tiến độ đi tới. Mở collection, rời vùng thao tác hoặc mất focus hủy gesture đang dở, không tự phát sinh cú bẻ.

Ice: `meltRadius = 0.92`, `meltPerSecond = 3.1`, tự hoàn tất khi average thaw đạt `0.76` để tránh kẹt mảnh nhỏ. Chỉ có một ngân sách tan trên mỗi frame, không được tăng tốc bằng spam drag event. Phần đã tan không tự đóng băng lại.

Bốn toy đang dùng artwork lấy từ `imageai`:

- Cotton Bunny: 1 coin/giây, starter và Basic pool.
- Cotton Dino: 2 coin/giây, Basic pool.
- Toast Cat: 5 coin/giây, Chocolate pool.
- Raincoat Cat: 15 coin/giây, Crystal và Ice pool.

Garden hiện là danh sách số bản và income, chưa phải garden 3D có nhiều toy hoạt động. Artwork toy là ảnh 2D trên quad. Collection có silhouette, owned count, 1–3 sao.

## 5. Tái sử dụng từ imageai

Nguồn: `Assets/_Egg/EggFragtureMesh`, `Imgs/rewards/NewCollection`, shader `RewardQuadReveal_URP` và flow trong `EggPcsMrg`, `RewardMgr`, `AchievementManager`, `UnfreezeFragment`, `EggPiecesByLevelSO`.

- Tên tier “50/100/200” thực tế có **52/101/200 MeshFilter**. Không bỏ mesh chỉ để khớp tên, vì vỏ sẽ bị khuyết.
- Mesh và transform được nhập vào `ImportedImageAI`; prefab nguồn đã bỏ component/plugin/tag ngoài phạm vi.
- `FractureBaked` là mesh normalized, pivot mỗi mảnh được recenter để Rigidbody xoay đúng.
- Runtime detach cụm gần điểm chạm; debris thường có trọng lực/impulse/torque, tự dọn sau 2 giây và không va chạm lẫn nhau.
- Ice dùng `MeltDebris` để thu nhỏ/trượt mảnh trong khoảng 0,45 giây.
- Không phụ thuộc DLL native fracture, quảng cáo, tải ảnh Google Drive hoặc feedback SDK từ project tham chiếu.

## 6. Render optics vừa được sửa

### Trước đó

Chỉ highlight trực tiếp từ đèn + shader opaque; chưa có môi trường reflection hoặc đường refraction. Không mô tả bản cũ như kính vật lý hoàn chỉnh.

### Hiện tại

- `ToyEconomy/MaterialSurface`: Basic nhám; Chocolate phản chiếu studio có roughness riêng, mặt cắt cocoa nhám hơn lớp ngoài.
- `ToyEconomy/OpticalGlass`: transparent pass, cubemap reflection/Fresnel, screen-space refraction, IOR, tint theo độ dày và dispersion.
- Băng: IOR **1.31**, normals tròn, frost blur giảm theo `_Heat`.
- Crystal: IOR **1.52**, reflection normals dạng mặt cắt; lens normal tương đối liên tục để toy không bị chia thành quá nhiều ảnh nhỏ.
- `Materials/StudioEnvironment.asset`: cubemap HDR studio có softbox, mip chain cho roughness. **Baked environment**, không capture mọi toy đang chuyển động.
- `Optical Studio Backdrop`: nền opaque có tương phản và contact-shadow cue để nhìn thấy hình bị méo qua kính.
- Camera có `UniversalAdditionalCameraData.requiresColorTexture = true` và `requiresDepthTexture = true`.
- Reward quad dùng `renderQueue = 2450` để nằm trong opaque color copy và xuất hiện qua kính. HUD không bị khúc xạ.
- `EggCrackController.Begin` gán `_EggCenter` cho vật liệu clone.
- Mặt tiếp giáp giữa hai cell còn gắn được ẩn bằng `OpticalCutFaceVisibility`; chỉ mặt cắt đã lộ ra mới được giữ lại. Các mảnh tách được khôi phục mặt cắt.

### Giới hạn cần nói đúng

Screen-space refraction chỉ nhìn được nội dung trong color copy; không truy vết vật thể ngoài màn hình hoặc nhiều lớp kính lồng nhau. Absorption/thickness là xấp xỉ. Reflection dùng cubemap studio, không phải realtime ray tracing. Chưa có profiling điện thoại cho overdraw của 200 fragment transparent và texture copy.

## 7. Bản đồ code

| File / class | Vai trò |
|---|---|
| `ToyDefinition`, `EggDefinition` | SO toy/egg, giá, pool, prefab, profile |
| `EggMaterialProfile` | Loại input, ngoài/trong, thông số hold/melt/impulse, instruction |
| `EconomyManager` | Wallet double, earn/spend |
| `ToyGarden` | Danh sách toy và tổng income, update theo frame |
| `ToyCollection` | Count theo toy ID và sao tối đa 3 |
| `EggShop` | Affordability và khóa trạng thái |
| `EggCrackController` | Bag reward, fragment, input/progress, physics, reveal event |
| `EggTapTarget` | Pointer ownership, down/up/click/drag, cancel |
| `GameLoopController` | Shop/Cracking/Reward, cộng toy, preview, Next |
| `HUD` | Shop 4 loại, instruction/progress, SIMPLE/SOUND, collection |
| `EggSoundFeedback` | Âm thanh prototype sinh bằng code, không phải sound design cuối |
| `MeltDebris` | Shrink/drift mảnh băng |
| `MaterialGameplaySetup` | Gắn 4 egg và profile gameplay vào scene trong Editor |
| `OpticalMaterialSetup` | Gắn shader/cubemap/camera/backdrop optics vào scene |
| `MaterialGameplayVerification` | 95 kiểm tra gameplay |
| `OpticalMaterialVerification` | Render / depth-order / volume / reward checks (số mới xem mục cuối) |

Các file `.meta` là một phần của asset graph. Giữ cùng asset khi di chuyển/sao chép.

## 8. Bằng chứng kiểm tra

Tất cả test dưới đây là Unity Editor Play Mode, không phải test trên Android/iOS.

| Chặng | Kết quả | Lưu ý |
|---|---|---|
| Core MVP | 57 checks pass | Lịch sử; cơ chế tap cũ |
| Fracture + collection | 417 checks pass | Lịch sử; 15/33/66 hit cũ, đã thay bởi material gameplay |
| Material gameplay | 95 checks pass, zero captured runtime errors | Pointer down/up/click/drag, hold, cancel, multi-pointer, melt, reward-once, Next, debris |
| Optics | 21 checks pass, zero captured runtime errors | Ảnh reflection on/off và refraction on/off; cả hai làm pixel thay đổi |
| Regression sau optics | 95 checks pass, zero captured runtime errors | Bộ gameplay chạy cùng scene/shader optics mới |

Latest artifacts trong Unity project:

- `Benchmarks/ToyEconomy/Optics/render-checks.json`
- `Benchmarks/ToyEconomy/Optics/gameplay-checks.json`
- `Benchmarks/ToyEconomy/Optics/{basic,chocolate,crystal,ice}.png`
- `Benchmarks/ToyEconomy/Optics/{crystal,ice}-no-reflection.png`
- `Benchmarks/ToyEconomy/Optics/{crystal,ice}-no-refraction.png`

Test harness thêm 1000 coin để đi nhanh qua các loại; **Play Mode bình thường không được thêm coin này**. Kiểm tra income vẫn chạy trên frame thật. Báo cáo lịch sử không được coi là pass cho một bản code mới nếu chưa chạy lại.

Workspace Codex có các bản chụp tương ứng: `ToyEconomyMVP`, `MaterialGameplay`, `OpticalMaterials`. Tài liệu chi tiết optics cũng có trong `Assets/ToyEconomy/OPTICAL_MATERIALS.md`.

## 9. Việc còn thiếu và thứ tự tiếp tục hợp lý

1. Cho người dùng đánh giá optics mới trong Editor chính; so với ảnh cũ, kiểm tra độ rõ reflection/refraction, khả năng nhận ra chất liệu và độ đọc của toy.
2. Nếu còn thiếu cảm giác vật liệu, chỉnh roughness, môi trường sáng, thickness, tint, chất lượng mesh/cut face dựa trên ảnh/video thực tế. Tránh thêm hiệu ứng quá mức làm toy khó nhìn.
3. Profile trên thiết bị mục tiêu: transparent overdraw, color/depth copy, frame time, bộ nhớ và nhiệt.
4. Theo yêu cầu tiếp theo: garden trực quan hơn, save/load/offline, progression gates và mở rộng danh mục toy.
5. Gỗ, jelly, gốm, kim loại và Crystal rhythm vẫn là thiết kế; chưa implement. Không tự mở rộng cả tám cơ chế cùng lúc.

Chưa có: save/offline income, haptics, âm thanh thu/thiết kế hoàn chỉnh, 12 toy, progression gates, projectile/hammer, monetization hoặc build/test phần cứng mobile.

## 10. Chạy tiếp bằng Claude

Mở Claude Code trong **Unity project chính**, gọi:

```text
/egg-unity-dev Đọc handoff, kiểm tra trạng thái Unity và tiếp tục cải thiện chất liệu từ bản reflection/refraction hiện tại. Giữ core loop và các thao tác đã có.
```

Skill không cài MCP, mở rộng permission hay tự deploy. Nếu Claude ở môi trường khác không có slash skill, gửi `SKILL.md`, file này và `GAME_PLAN.md` cho Claude cùng quyền truy cập project; không chỉ gửi ảnh/báo cáo rồi kỳ vọng Claude có source code.

## 11. Material polish — 17/10/2026

- Ice có frost pockets từ noise liên tục, vân đông và bọt khí nhỏ. `_Heat` của từng fragment làm tan các chi tiết này, giảm blur để toy rõ dần. Giữ nguyên reflection/refraction, IOR và gameplay. Crystal không đổi trong lượt này.
- Thay đổi: `Shaders/OpticalGlass.shader`, `Runtime/OpticalMaterialVerification.cs`.
- Kiểm tra mới: **22/22 render**, **95/95 gameplay**, zero captured errors (trước bản depth fix bên dưới). Ảnh đông/tan là so sánh `_Heat=0` và `0.85` toàn vỏ trong render harness; gameplay suite riêng kiểm tra thao tác rub theo vùng.
- Artifact mới: `Benchmarks/ToyEconomy/MaterialPolish/{render-checks.json,gameplay-checks.json,optical-ice.png,optical-ice-thawed.png}`. Chênh lệch ảnh đông/tan khoảng `0.02055` trung bình RGB chuẩn hóa trong vùng trứng; chứng minh đường render hoạt động, không phải art approval.
- Chưa profile GPU/mobile; shader noise mới thêm phép tính, cần đo trên thiết bị trước khi chốt chất lượng. Không thêm vật liệu mới hoặc thay đổi economy trong lượt này.

## 12. Vỏ dày và crystal depth fix — 17/10/2026

- Phản hồi: toy quá lộ; mặt trong crystal phủ sai lên reward/mặt trước. Đã tăng optical thickness Crystal 1.8, Ice 1.6 và density 0.65/1.05. Đây là độ dày quang học, không remesh. Ice trong dần theo heat.
- Glass `ZWrite On`; reward đặt `_ZWrite=1`, shader clip pixel alpha gần 0 để không tạo depth hình chữ nhật. Vẫn lấy refraction từ opaque color copy.
- `OpticalCutFaceVisibility` ẩn cut triangles chung mặt phẳng giữa cell còn gắn; tách cell sẽ khôi phục mặt cắt và cập nhật phần còn lại. Mesh clone được dọn bằng lifetime component. Bỏ cách hiện tất cả mặt trong sau cú đập đầu.
- Hoàn tất egg tắt renderer của debris để màn thưởng không bị che, vẫn giữ physics và cleanup theo lịch.
- Mới nhất: **32 render checks + 95 gameplay checks PASS, zero captured errors** trong Editor Play Mode trên bản tạm. Đảo sortingOrder sau khi debris rơi hết có ảnh không thay đổi ở cả Ice/Crystal. Có ảnh intact / thin comparison / partial / partial-settled / reward.
- Artifact: `Benchmarks/ToyEconomy/CrystalDepthFix/`. Source mới: OpticalGlass, imported RewardQuadReveal_URP (thêm depth option mặc định tắt), GameLoopController, EggCrackController, OpticalCutFaceVisibility, OpticalMaterialSetup, OpticalMaterialVerification và bốn optical materials.
- Giới hạn: matching mặt phẳng có lượng tử hóa, cần kiểm tra lại nếu thay bộ mesh fracture. Chưa profile runtime mesh allocation/update hoặc GPU trên mobile.
