// Copyright (C) 2024 Claudia Wagner

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Loginator.Infrastructure.UnitTests.Option {

    /// <summary>
    /// Represents test data for <see cref="OptionsRepositoryTests"/>.
    /// </summary>
    internal class OptionsRepositoryTestData {

        private enum Infixes {
            MISSING = 0,
            EMPTY,
            FULL,
            NO_PERSON,
            NULL_PERSON,
            NO_ADDRESS,
            NULL_ADDRESS,
            NO_STREET,
            NULL_STREET,
            NO_NOTE,
            NULL_NOTE,
            NO_NOTES,
            NULL_NOTES,
        }

        private static readonly string[] INFIXES = [
            "missing",
            "empty",
            "full",
            "noPerson", "nullPerson",
            "noAddress", "nullAddress",
            "noStreet", "nullStreet",
            "noNote", "nullNote",
            "noNotes", "nullNotes"
        ];

        public static IEnumerable FirstLayerTestCases =>
            INFIXES.Take(5);

        public static IEnumerable SecondLayerTestCases =>
            INFIXES.Take(7);

        public static IEnumerable ThirdLayerTestCases =>
            INFIXES.Take(9);

        public static IEnumerable FourthLayerTestCases =>
            INFIXES.Take(11);

        public static IEnumerable FourthLayerListTestCases =>
            INFIXES.Take(9).Concat([
                INFIXES[(int)Infixes.NO_NOTES],
                INFIXES[(int)Infixes.NULL_NOTES]
            ]);

        public static Person CreatePerson() =>
            new() {
                FirstName = "FirstName",
                LastName = "LastName",
                Address = CreateAddress()
            };

        public static Address CreateAddress() =>
            new() {
                Street = CreateStreet(),
                Postcode = 543210,
            };

        public static Street CreateStreet() =>
            new() {
                Line1 = "Line1",
                Line2 = "Line2",
                Line3 = "Line3",
                Note = CreateNote(),
                Notes = [
                    CreateNote(),
                ],
            };

        public static Note CreateNote() =>
            new() {
                Text = "Text",
                Number = 543210,
                Other = "Other",
            };


        public static Notes CreateNotes() =>
            [
                CreateNote(),
                CreateNote(),
            ];

        public interface IAppOptions<TOptions>
            where TOptions : class, new() {

            void CopyTo(TOptions destination);

            AppOptions Merge(AppOptions? options);
        }

        public record AppOptions {
            public string Other { get; set; } = string.Empty;
            public Person? Person { get; set; }
        }

        public record Person : IAppOptions<Person> {

            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public Address? Address { get; set; }

            public void CopyTo(Person destination) {
                destination.FirstName = this.FirstName;
                destination.LastName = this.LastName;
                destination.Address = this.Address;
            }

            public AppOptions Merge(AppOptions? options) {
                options ??= new AppOptions();
                options.Person = this;
                return options;
            }
        }

        public record Address : IAppOptions<Address> {

            public Street? Street { get; set; }
            public int Postcode { get; set; }

            public void CopyTo(Address destination) {
                destination.Street = this.Street;
                destination.Postcode = this.Postcode;
            }

            public AppOptions Merge(AppOptions? options) {
                options ??= new AppOptions();
                options.Person ??= new Person();
                options.Person.Address = this;
                return options;
            }
        }

        public record Street : IAppOptions<Street> {

            public string Line1 { get; set; } = string.Empty;
            public string Line2 { get; set; } = string.Empty;
            public string Line3 { get; set; } = string.Empty;
            public Note? Note { get; set; }
            public List<Note>? Notes { get; set; } = [];

            public void CopyTo(Street destination) {
                destination.Line1 = this.Line1;
                destination.Line2 = this.Line2;
                destination.Line3 = this.Line3;
                destination.Note = this.Note;
                destination.Notes = this.Notes;
            }

            public AppOptions Merge(AppOptions? options) {
                options ??= new AppOptions();
                options.Person ??= new Person();
                options.Person.Address ??= new Address();
                options.Person.Address.Street = this;
                return options;
            }
        }

        public record Note : IAppOptions<Note> {

            public string Text { get; set; } = string.Empty;
            public int Number { get; set; } = 0;
            public string Other { get; set; } = string.Empty;

            public void CopyTo(Note destination) {
                destination.Text = this.Text;
                destination.Other = this.Other;
                destination.Number = this.Number;
            }

            public AppOptions Merge(AppOptions? options) {
                options ??= new AppOptions();
                options.Person ??= new Person();
                options.Person.Address ??= new Address();
                options.Person.Address.Street ??= new Street();
                options.Person.Address.Street.Note = this;
                return options;
            }
        }

        public class Notes : List<Note>, IAppOptions<Notes> {

            public void CopyTo(Notes destination) {
                destination.Clear();
                destination.AddRange(this);
            }

            public AppOptions Merge(AppOptions? options) {
                options ??= new AppOptions();
                options.Person ??= new Person();
                options.Person.Address ??= new Address();
                options.Person.Address.Street ??= new Street();
                options.Person.Address.Street.Notes = this;
                return options;
            }
        }
    }
}