/****** Para agregar los módulos  ******/
INSERT INTO [ptstools_HelpDesk].[dbo].[Modulo] ([nombre])
VALUES
('Cálculo FCV'),
('Conv. Tabla de cubicación'),
('SLA'),
('Roles Y Privilegios');
GO

/****** Para agregar las nuevas operaciones ******/
INSERT INTO [ptstools_HelpDesk].[dbo].[Operacion] ([nombre], [id_Modulo])
VALUES
('Mostrar_Manuales', 5),
('Filtrar_Ticket', 2),
('Mostrar_Calc_FCV', 6),
('Mostrar_Tabla_Cu', 7),
('Mostrar_SLA', 8),
('Generar_Reporte_SLA', 8),
('Ver_detalles_SLA', 8),
('Mostrar_Rol_Y_Priv', 9),
('Agregar_Operacion', 9),
('Editar_Operacion', 9),
('Eliminar_Operacion', 9),
('Agregar_Modulo', 9),
('Editar_Modulo', 9),
('Eliminar_Modulo', 9),
('Agregar_Rol', 9),
('Editar_Rol', 9),
('Eliminar_Rol', 9),
('Agregar_Privilegios', 9),
('Exportar_Tickets', 1);
GO

DELETE FROM [ptstools_HelpDesk].[dbo].[Roll_Operacion]
WHERE (id_Roll = 3 AND id_Operacion IN (10, 14))
   OR (id_Roll = 2 AND id_Operacion = 21);

/****** Para agregar la relación rol-privilegio con las nuevas operaciones******/
INSERT INTO [ptstools_HelpDesk].[dbo].[Roll_Operacion] ([id_Roll], [id_Operacion])
VALUES
(1, 23),
(1, 22),
(1, 24),
(1, 25),
(1, 26),
(1, 27),
(1, 28),
(1, 29),
(1, 30),
(1, 31),
(1, 32),
(1, 33),
(1, 34),
(1, 35),
(1, 36),
(1, 37),
(1, 38),
(1, 39),
(1, 40),
(3, 23),
(3, 22),
(3, 26),
(3, 1),
(3, 7),
(3, 25),
(3, 24),
(4, 5),
(4, 25),
(4, 16),
(4, 22),
(4, 24),
(2, 2),
(2, 17),
(2, 23),
(2, 22),
(2, 24),
(2, 25),
(2, 26);
GO

------------------------------------------------------------------------------
