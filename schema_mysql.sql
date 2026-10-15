-- =====================================================================
-- Base de Datos: stellumdb
-- Motor: MySQL 8.0+ / MariaDB 10.4+
-- Descripción: Tablas para índice permanente de PDFs de origen y bitácora TIFF
-- =====================================================================

CREATE DATABASE IF NOT EXISTS `stellumdb`
  DEFAULT CHARACTER SET utf8mb4
  DEFAULT COLLATE utf8mb4_unicode_ci;

USE `stellumdb`;

-- ---------------------------------------------------------------------
-- 1. TABLA: indice_pdf_origen
-- Diccionario e índice permanente de archivos PDF de origen.
-- Guarda el número real de páginas, tamaño y fecha para evitar abrir
-- los PDFs a través de la red SMB cada vez que se analiza o procesa.
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `indice_pdf_origen` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `ruta_completa` VARCHAR(1000) NOT NULL COMMENT 'Ruta completa UNC/SMB o física del PDF de origen',
    `nombre` VARCHAR(255) NOT NULL COMMENT 'Nombre del archivo PDF con extensión',
    `paginas` INT NOT NULL DEFAULT 0 COMMENT 'Número total de páginas del PDF',
    `tamano` BIGINT NOT NULL DEFAULT 0 COMMENT 'Tamaño en bytes del archivo al indexar',
    `fecha_mod` DATETIME NULL COMMENT 'Fecha de última modificación del archivo',
    `estatus` VARCHAR(20) NOT NULL DEFAULT 'OK' COMMENT 'OK o ERROR si el PDF no pudo leerse',
    `fecha_indexado` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Momento en que se registró en el índice',
    PRIMARY KEY (`id`),
    INDEX `idx_ruta` (`ruta_completa`(190)),
    INDEX `idx_nombre` (`nombre`),
    INDEX `idx_estatus_paginas` (`estatus`, `paginas`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Índice permanente de PDFs de origen';

-- ---------------------------------------------------------------------
-- 2. TABLA: bitacora_transferencia_tiff
-- Bitácora de entregables procesados a formato TIFF (250 DPI LZW Grayscale).
-- Registra los documentos convertidos con éxito o con error.
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `bitacora_transferencia_tiff` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `disk` VARCHAR(50) DEFAULT NULL COMMENT 'Identificador de disco o unidad',
    `carpeta_origen` VARCHAR(1000) DEFAULT NULL COMMENT 'Ruta de la carpeta origen del documento',
    `carpeta_destino` VARCHAR(1000) DEFAULT NULL COMMENT 'Ruta de la subcarpeta destino de los TIFFs',
    `documento` VARCHAR(255) NOT NULL COMMENT 'Nombre del documento o identificador',
    `nombre_original` VARCHAR(255) DEFAULT NULL COMMENT 'Nombre original del archivo PDF',
    `nombre_nuevo` VARCHAR(255) DEFAULT NULL COMMENT 'Nombre asignado al entregable',
    `error` TEXT DEFAULT NULL COMMENT 'Mensaje de error si falló',
    `estatus` VARCHAR(50) NOT NULL DEFAULT 'OK' COMMENT 'OK o ERROR',
    `paginas` INT NOT NULL DEFAULT 0 COMMENT 'Páginas/imágenes TIFF generadas',
    `delegacion` VARCHAR(100) DEFAULT NULL COMMENT 'Delegación o municipio',
    `usuario` VARCHAR(100) DEFAULT NULL COMMENT 'Usuario del sistema',
    `anio` VARCHAR(10) DEFAULT NULL COMMENT 'Año del documento',
    `fecha` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Fecha de conversión',
    PRIMARY KEY (`id`),
    INDEX `idx_documento_tiff` (`documento`),
    INDEX `idx_nombre_original_tiff` (`nombre_original`),
    INDEX `idx_estatus_tiff` (`estatus`),
    INDEX `idx_carpeta_destino_tiff` (`carpeta_destino`(190))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Bitácora de conversión a TIFF';
