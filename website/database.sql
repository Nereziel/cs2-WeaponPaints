-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: localhost
-- Generation Time: Nov 02, 2023 at 11:12 AM
-- Server version: 10.11.2-MariaDB
-- PHP Version: 8.2.3

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `cs2server`
--

-- --------------------------------------------------------

--
-- Table structure for table `wp_player_skins`
--

CREATE TABLE `wp_player_skins` (
  `steamid` varchar(64) NOT NULL,
  `weapon_defindex` int(6) NOT NULL,
  `weapon_paint_id` int(6) NOT NULL,
  `weapon_wear` float NOT NULL DEFAULT 0.0001,
  `weapon_seed` int(16) NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_unicode_ci;

--
-- Table structure for table `wp_player_knife`
--

CREATE TABLE `wp_player_knife` (
  `steamid` varchar(64) NOT NULL,
  `knife` varchar(64) NOT NULL,
  UNIQUE (`steamid`)
) ENGINE = InnoDB;
