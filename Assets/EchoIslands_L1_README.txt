ECHO ISLANDS - ENVIRONMENT FREE / LEVEL 1

Что добавлено
- Environment_Free_L1.unity - копия красивой карты Polytope Studio, подготовленная как первый уровень.
- WorldVariant - простой компонент для назначения объекту мира Both / Light / Dark.
- WorldVariantEditor - удобный Inspector с кнопками Оба / Свет / Тьма.
- WorldVariantMenu - быстрые команды Echo Islands -> Мир и открытие L1.
- EnvironmentFreeL1Bootstrap - стартовая сборка посоха и портала.
- L1ExitPortal - завершение первого уровня через портал.

Как открыть
1. Откройте проект в Unity 6000.3.0f1.
2. В меню Unity выберите Echo Islands -> Уровень 1 -> Открыть Environment Free L1.
3. Нажмите Play.

Управление
- WASD - движение
- мышь - обзор
- Space - прыжок
- E - подобрать посох / взаимодействовать
- ЛКМ - переключить мир после получения посоха
- Escape - освободить / вернуть курсор

Как назначить объект второму миру
1. Откройте Environment_Free_L1.unity.
2. Выберите любой объект в Hierarchy, например мост, руины, дверь или декор.
3. В Inspector нажмите Add Component -> World Variant.
4. Выберите кнопку Тьма.
5. Collider и Renderer объекта будут включаться только в Мертвом мире.

Еще быстрее
Echo Islands -> Мир -> Выбранное: Тьма
Echo Islands -> Мир -> Выбранное: Свет
Echo Islands -> Мир -> Выбранное: Оба

Важно
- Не ставьте World Variant на весь Terrain. Основной ландшафт должен быть общим для обоих миров.
- Для группы объектов удобно повесить World Variant на родительский GameObject и оставить "Применять к дочерним объектам".
- Для мостов, руин, дверей, обломков и декоративных элементов используйте отдельные объекты/группы.

Расположение маркеров
В Hierarchy внутри Echo_L1_SystemRoot:
- L1_StaffSpawn - место посоха.
- L1_PortalSpawn - место портала.
Их можно просто перетащить в нужные места прямо в Scene. Положение по Y не обязательно выставлять точно: уровень сам ищет землю лучом сверху.

Про сцену
Оригинальная Environment_Free.unity не изменена. Игровая версия находится рядом:
Environment_Free_L1.unity

Демо-скрипты Polytope PlayerMovement и MouseLook в L1-сцене отключены, а используется FirstPersonController проекта.
