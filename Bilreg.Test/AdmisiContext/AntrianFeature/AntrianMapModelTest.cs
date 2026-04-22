using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianMapModelTest
{
	public class CreateTests
	{
		[Fact]
		public void Constructor_ShouldInitializeProperties_WhenInputIsValid()
		{
			// Arrange
			var detils = new[]
			{
				CreateDetil(1, isTerpakai: false, flag: "N"),
				CreateDetil(3, isTerpakai: true, flag: "A"),
				CreateDetil(2, isTerpakai: false, flag: "A")
			};

			var model = new AntrianMapModel(
				antrianMapId: "MAP1",
				jadwalId: "J1",
				dokter: new PpaReff("D1","Dr Test"),
				layanan: new LayananReff("L1","Poli"),
				tglJadwal: new DateOnly(2026,1,1),
				jamJadwal: new TimeOnly(8,0),
				jamPraktek: new TimeOnly(8,30),
				pattern: "P",
				maxPasien: 10,
				listMap: detils
			);

			// Assert
			model.AntrianMapId.Should().Be("MAP1");
			model.JadwalId.Should().Be("J1");
			model.DokterId.Should().Be("D1");
			model.LayananId.Should().Be("L1");
			model.TglJadwal.Should().Be(new DateOnly(2026,1,1));
			model.MaxPasien.Should().Be(10);
			model.TotalSlotCount.Should().Be(3);
			model.LastNoUrut.Should().Be(3);
		}

		[Fact]
		public void Constructor_ShouldUseEmptyList_WhenListMapIsNull()
		{
			// Arrange & Act
			var model = new AntrianMapModel(
				antrianMapId: "MAP2",
				jadwalId: "J2",
				dokter: new PpaReff("D2","Dr X"),
				layanan: new LayananReff("L2","Poli2"),
				tglJadwal: DateOnly.FromDateTime(DateTime.Today),
				jamJadwal: TimeOnly.FromDateTime(DateTime.Now),
				jamPraktek: TimeOnly.FromDateTime(DateTime.Now),
				pattern: "P",
				maxPasien: 5,
				listMap: null
			);

			// Assert
			model.TotalSlotCount.Should().Be(0);
			// Accessing LastNoUrut when there are no slots should throw (behavior of Max over empty sequence)
			Action act = () => { var _ = model.LastNoUrut; };
			act.Should().Throw<InvalidOperationException>();
		}
	}

	public class PropertyTests
	{
		[Fact]
		public void Default_ShouldHaveDefaults_WhenAccessed()
		{
			// Arrange & Act
			var def = AntrianMapModel.Default;

			// Assert
			def.AntrianMapId.Should().Be("-");
			def.MaxPasien.Should().Be(0);
			def.TotalSlotCount.Should().Be(0);
			Action act = () => { var _ = def.LastNoUrut; };
			act.Should().Throw<InvalidOperationException>();
		}

		[Fact]
		public void Key_ShouldReturnKeyWithGivenId_WhenCalled()
		{
			// Act
			var key = AntrianMapModel.Key("X123");

			// Assert
			key.AntrianMapId.Should().Be("X123");
		}
	}

	public class BehaviorTests
	{
		[Fact]
		public void GetNextAntrian_ShouldReturnFirstAvailableSlot_WhenMatchingFlagExists()
		{
			// Arrange
			var detils = new List<AntrianMapDetilModel>
			{
				CreateDetil(1, isTerpakai: true, flag: "A"),
				CreateDetil(2, isTerpakai: false, flag: "B"),
				CreateDetil(3, isTerpakai: false, flag: "A")
			};

			var model = new AntrianMapModel("m", "j", new PpaReff("d","n"), new LayananReff("l","n"),
				DateOnly.MinValue, TimeOnly.MinValue, TimeOnly.MinValue, "p", 1, detils);

			// Act
			var next = model.GetNextAntrian("A");

			// Assert: should return the lowest NoUrut among unused slots with flag A (3 in this case)
			next.Should().NotBeNull();
			next.NoUrut.Should().Be(3);
			next.Flag.Should().Be("A");
			next.IsTerpakai.Should().BeFalse();
		}

		[Fact]
		public void GetNextAntrian_ShouldReturnAutoSlotWithNextNoUrut_WhenNoAvailableMatchingSlot()
		{
			// Arrange: existing slots but none available for flag "Z"
			var detils = new List<AntrianMapDetilModel>
			{
				CreateDetil(1, isTerpakai: true, flag: "A"),
				CreateDetil(2, isTerpakai: true, flag: "B")
			};

			var model = new AntrianMapModel("m2", "j2", new PpaReff("d2","n2"), new LayananReff("l2","n2"),
				DateOnly.MinValue, TimeOnly.MinValue, TimeOnly.MinValue, "p", 2, detils);

			// Act
			var next = model.GetNextAntrian("Z");

			// Assert: should be an auto slot with NoUrut = LastNoUrut + 1
			next.Should().NotBeNull();
			next.Flag.Should().Be("AUTO");
			next.NoUrut.Should().Be(model.LastNoUrut + 1);
		}

		[Fact]
		public void VoidSlot_ShouldConvertSlotToAuto_WhenSlotExists()
		{
			// Arrange
			var detil = CreateDetil(5, isTerpakai: true, flag: "X");
			var list = new List<AntrianMapDetilModel> { detil };
			var model = new AntrianMapModel("m3", "j3", new PpaReff("d3","n3"), new LayananReff("l3","n3"),
				DateOnly.MinValue, TimeOnly.MinValue, TimeOnly.MinValue, "p", 1, list);

			// Act
			model.VoidSlot(5);

			// Assert: detil should have been voided (flag AUTO and ReffId set to "-")
			var updated = model.ListMap.Should().ContainSingle().Which;
			updated.Flag.Should().Be("AUTO");
			updated.ReffId.Should().Be("-");
			updated.PasienId.Should().Be(PasienModel.Default.ToReff().PasienId);
		}

		[Fact]
		public void VoidSlot_ShouldDoNothing_WhenSlotNotFound()
		{
			// Arrange
			var detil = CreateDetil(7, isTerpakai: false, flag: "Y");
			var list = new List<AntrianMapDetilModel> { detil };
			var model = new AntrianMapModel("m4", "j4", new PpaReff("d4","n4"), new LayananReff("l4","n4"),
				DateOnly.MinValue, TimeOnly.MinValue, TimeOnly.MinValue, "p", 1, list);

			// Act
			Action act = () => model.VoidSlot(999);

			// Assert: no exception and original slot unchanged
			act.Should().NotThrow();
			model.ListMap.Should().ContainSingle(x => x.NoUrut == 7 && x.Flag == "Y");
		}
	}

	// Helper factory for detil records to keep tests readable
	private static AntrianMapDetilModel CreateDetil(int noUrut, bool isTerpakai, string flag)
	{
		var pasien = new PasienReff($"P{noUrut}", $"Pasien{noUrut}", new DateOnly(1990,1,1), "M");
		return new AntrianMapDetilModel(noUrut, pasien.PasienName, pasien.PasienId, reffId: $"REF{noUrut}", flag: flag, isTerpakai: isTerpakai);
	}
}