ECHO ISLANDS - VISUAL OVERHAUL

Это патч для текущего проекта из присланного Assets.zip.
Распакуйте содержимое архива поверх корня Unity-проекта, чтобы папка Assets слилась с вашей Assets.

Что обновлено:
- Светлый мир: мягкая пастельная атмосфера, уменьшенная яркость, розово-золотые акценты, звездная пыль.
- Темный мир: почти черный фон/освещение, ярко-бирюзовая энергия, скалк-подобные частицы и процедурные прожилки в Echo/DualWorldLit.
- Переключение: плавное изменение blend, экранное искажение, хроматическая аберрация, вспышка, пространственная волна, кольца и частицы от игрока.
- WorldVariant: мир-специфичные объекты теперь могут плавно появляться и исчезать во фронте перехода.
- E-подбор: посох и сфера летят к точке удержания с анимацией и вспышкой частиц.
- Сфера: при помещении в сокет есть отдельная анимация прилета, вспышка и активация портала.
- Портал: энергия появляется через отдельный VFX, расширяется, заполняется и затем открывает переход на следующий уровень при входе.
- L1 Environment Free: портал первого уровня теперь требует одну сферу; посох только открывает доступ к переключению миров.

Важно:
Visual systems создаются автоматически на объекте WorldState, поэтому отдельные компоненты в Main.unity вручную добавлять не нужно.

Для мир-специфичного объекта:
1. Выделите объект.
2. Add Component -> World Variant.
3. В Inspector выберите Both / Light / Dark.
4. Оставьте "Менять объект волной переключения" и "Плавное появление / исчезновение" включенными.

Основные файлы:
Assets/Scripts/World/WorldState.cs
Assets/Scripts/World/WorldAtmosphere.cs
Assets/Scripts/World/WorldTransitionVFX.cs
Assets/Scripts/World/WorldShiftImageEffect.cs
Assets/Scripts/World/WorldAmbientVFX.cs
Assets/Scripts/World/WorldVariant.cs
Assets/Scripts/World/PortalActivationVFX.cs
Assets/Scripts/Puzzle/ToolPickup.cs
Assets/Scripts/Puzzle/Orb.cs
Assets/Scripts/Puzzle/OrbSocket.cs
Assets/Scripts/Puzzle/PortalFrame.cs
Assets/Scripts/L1/EnvironmentFreeL1Bootstrap.cs
Assets/Shaders/WorldShift.shader
Assets/Shaders/DualWorldLit.shader
